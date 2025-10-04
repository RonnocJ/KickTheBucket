using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SongName
{
    Test
}

public class MusicManager : Singleton<MusicManager>
{
    [System.Serializable]
    public class Song
    {
        public SongName name;
        public AudioClip track;
        public float bpm;
        public float startBeats;
        public float endBeats;
        public float StartSeconds => startBeats * 60f / Mathf.Max(1f, bpm);
        public float EndSeconds => endBeats * 60f / Mathf.Max(1f, bpm);
    }

    [SerializeField] private Song[] songs;
    [SerializeField] private float dspLead = 0.05f; // scheduling lead-time so PlayScheduled works cleanly

    public readonly List<AudioSource> liveSources = new List<AudioSource>();
    private Dictionary<SongName, Song> _songDict = new();

    private void Start()
    {
        foreach (var s in songs)
            _songDict[s.name] = s;

        StartPlaybackLoop(SongName.Test);
    }

    public void StartPlaybackLoop(SongName songName)
    {
        var song = _songDict[songName];

        if (song.track == null)
        {
            Debug.LogError("MusicManager: no AudioClip assigned on song.track");
            return;
        }

        if (song.endBeats <= song.startBeats)
        {
            Debug.LogError("MusicManager: endBeats must be greater than startBeats");
            return;
        }

        // schedule the first instance to play from the very beginning (0s)
        double dspStart = AudioSettings.dspTime + dspLead;
        var firstSrc = ScheduleNewInstanceAt(song, dspStart, startFromBeginning: true);

        // Start a coroutine that watches this instance and schedules the *next* dynamically,
        // which will in turn watch that next one and schedule the next, etc.
        StartCoroutine(WatchAndScheduleChain(song, firstSrc, dspStart));
    }

    // Schedule an AudioSource instance at dspTime; returns the AudioSource.
    private AudioSource ScheduleNewInstanceAt(Song song, double dspTime, bool startFromBeginning)
    {
        var go = new GameObject("MusicInstance");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();

        src.clip = song.track;
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;

        int startSamples = startFromBeginning ? 0 : Mathf.RoundToInt(song.StartSeconds * song.track.frequency);
        src.timeSamples = Mathf.Clamp(startSamples, 0, song.track.samples - 1);

        // schedule to start at dspTime. PlayScheduled is sample-accurate.
        src.PlayScheduled(dspTime);

        liveSources.Add(src);
        // destroy when full clip length has elapsed from dspTime (so baked reverb / tails finish)
        StartCoroutine(DestroyWhenFinished(src, dspTime + song.track.length));

        return src;
    }

    // Watches a playing AudioSource and schedules the next one exactly when its playback reaches EndSeconds,
    // adapting dynamically to changes in src.time and src.pitch.
    private IEnumerator WatchAndScheduleChain(Song song, AudioSource watchedSrc, double watchedStartDSP)
    {
        // Wait until it actually starts (dsp moment)
        while (AudioSettings.dspTime < watchedStartDSP)
            yield return null;

        // The watchedSrc.time is valid now and will move according to src.pitch
        // We'll continuously compute remaining playback content and schedule the next instance
        // with a small dspLead to let PlayScheduled be used.
        while (true)
        {
            if (watchedSrc == null)
                yield break;

            // Defensive: if source stopped or destroyed, bail out
            if (!watchedSrc.isPlaying && watchedSrc.time <= 0f)
            {
                yield break;
            }

            // compute how much content (in clip-seconds) remains until EndSeconds
            // Use watchedSrc.time (clip playback position, respects pitch changes)
            float currentClipTime = watchedSrc.time;
            float remainingContentSeconds = song.EndSeconds - currentClipTime;

            // If we've already passed EndSeconds (or are at it), schedule immediately
            if (remainingContentSeconds <= 0f)
            {
                double scheduleDSP = AudioSettings.dspTime + dspLead;
                ScheduleNewInstanceAt(song, scheduleDSP, startFromBeginning: false);
                // start watching the new instance and stop watching old one
                var newSrc = liveSources[liveSources.Count - 1];
                StartCoroutine(WatchAndScheduleChain(song, newSrc, scheduleDSP));
                yield break;
            }

            // src.time advances at a rate dependent on src.pitch.
            // Estimate DSP time until EndSeconds using current pitch.
            float effectivePitch = Mathf.Max(0.0001f, watchedSrc.pitch); // guard against 0
            float timeToEndDSP = remainingContentSeconds / effectivePitch;

            // If timeToEndDSP is short enough that we must schedule the next now (to allow dspLead),
            // then schedule it for AudioSettings.dspTime + dspLead.
            if (timeToEndDSP <= dspLead + 0.005f)
            {
                double scheduleDSP = AudioSettings.dspTime + dspLead;
                ScheduleNewInstanceAt(song, scheduleDSP, startFromBeginning: false);
                // start watching the new instance and stop watching old one
                var newSrc = liveSources[liveSources.Count - 1];
                StartCoroutine(WatchAndScheduleChain(song, newSrc, scheduleDSP));
                yield break;
            }

            // otherwise keep looping and re-check each frame (this will adapt if pitch changes)
            yield return null;
        }
    }

    private IEnumerator DestroyWhenFinished(AudioSource src, double destroyDSPTime)
    {
        while (AudioSettings.dspTime < destroyDSPTime)
            yield return null;

        liveSources.Remove(src);
        if (src.isPlaying) src.Stop();
        Destroy(src.gameObject);
    }

    private void OnDisable()
    {
        foreach (var src in liveSources.ToArray())
        {
            if (src == null) continue;
            if (src.isPlaying) src.Stop();
            Destroy(src.gameObject);
        }
        liveSources.Clear();
    }
}
