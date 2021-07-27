using UnityEngine;

namespace RTSToolkit
{
    public class MusicManager : MonoBehaviour
    {
        public AudioClip nightMusic;
        public AudioClip morningMusic;
        public AudioClip middayMusic;
        public AudioClip eveningMusic;

        public float nightMusicStartTime;
        public float morningMusicStartTime;
        public float middayMusicStartTime;
        public float eveningMusicStartTime;

        AudioSource musicAudioSource;
        bool isSet = false;

        void Start()
        {
            isSet = false;

            if (TimeOfDay.active == null)
            {
                this.enabled = false;
            }

            if (RTSCamera.active == null)
            {
                this.enabled = false;
            }

            if (this.enabled)
            {
                GameObject go = new GameObject("CameraMusic");
                go.transform.SetParent(RTSCamera.active.gameObject.transform);

                musicAudioSource = go.AddComponent<AudioSource>();
                AudioClip clipToPlay = PickClipByDayTime(TimeOfDay.active.currentDayTimeHrs);

                if (clipToPlay != null)
                {
                    musicAudioSource.clip = clipToPlay;
                    musicAudioSource.Play();
                }

                isSet = true;
            }
        }

        void Update()
        {
            if (isSet)
            {
                if (musicAudioSource.isPlaying == false)
                {
                    AudioClip clipToPlay = PickClipByDayTime(TimeOfDay.active.currentDayTimeHrs);

                    if (clipToPlay != null)
                    {
                        musicAudioSource.clip = clipToPlay;
                        musicAudioSource.Play();
                    }
                }
            }
        }

        AudioClip PickClipByDayTime(float t)
        {
            if ((t >= nightMusicStartTime) && (t < morningMusicStartTime))
            {
                return nightMusic;
            }

            if ((t >= morningMusicStartTime) && (t < middayMusicStartTime))
            {
                return morningMusic;
            }

            if ((t >= middayMusicStartTime) && (t < eveningMusicStartTime))
            {
                return middayMusic;
            }

            if ((t >= eveningMusicStartTime) && (t < nightMusicStartTime))
            {
                return eveningMusic;
            }

            return null;
        }
    }
}
