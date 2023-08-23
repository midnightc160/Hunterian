using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CognitiveServices.Speech;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MyAssets.Scripts.Speech
{
    public class TextToSpeech : MonoBehaviour
    {
        public AudioSource audioSource;

        [SerializeField] private string subscriptionKey = "ID";
        [SerializeField] private string region = "Region";
        private string soundSetting = "en-US-TonyNeural";


        // 频率为44100
        private const int SampleRate = 24000;
        private object threadLocker = new object();
        private bool waitingForSpeak;
        private bool audioSourceNeedStop;
        private string message;


        // 语音配置
        private SpeechConfig speechConfig;

        // 语音合成器
        private SpeechSynthesizer synthesizer;


        // 角色面部动画
        public SkinnedMeshRenderer skinnedMeshRenderer;

        // 39开始是aiueo的发音
        public int blendShapeIndex;

        // 嘴部动画速度，180f比较自然
        public float animationSpeed = 180f;

        // 动画范围，99意思是0到99
        public float animationRange = 99;

        // 语音合成的时长，每次合成后会被赋值
        private float audioDuration;

        // 语音合成的时长是否已经设置，因为synthesizer.SynthesisCompleted事件执行会晚一点，所以需要等待该事件执行完毕后再播放音频
        bool isDurationSet = false;

        // 时间偏移，用于调整嘴部动画播放，-0.8f比较自然  
        public float timeOffset = -0.6f;

        // 设置下拉UI用于切换语音
        [SerializeField] private TMP_Dropdown ChangeLanguageDropdown;

        private void Start()
        {
            // 创建语音配置
            speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            // 设置语音合成输出格式
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm);
            // 设置声音
            speechConfig.SpeechSynthesisVoiceName = soundSetting;

            CreatSynthesizer();


            ChangeLanguageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }


        // 创建语言合成器
        public void CreatSynthesizer()
        {
            // 创建一个语音合成器。
            // 请确保在使用完合成器后将其处理掉!
            synthesizer = new SpeechSynthesizer(speechConfig, null);
            // 语音合成取消时触发的事件
            synthesizer.SynthesisCanceled += (s, e) =>
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(e.Result);
                Debug.Log(
                    $"CANCELED:\nReason=[{cancellation.Reason}]\nErrorDetails=[{cancellation.ErrorDetails}]\nDid you update the subscription info?");
            };

            // 合成结束后获取语音合成的时长
            synthesizer.SynthesisCompleted += (s, e) =>
            {
                var result = e.Result;
                var duration = result.AudioDuration;
                audioDuration = (float)duration.TotalSeconds;
                isDurationSet = true;
            };
        }


        /// <summary>
        /// 语音合成
        /// </summary>
        /// <param name="input">需要转为语音的文本</param>
        public void AzureTextToSpeech(string input)
        {
            // 如果正在播放，停止播放
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // 锁定线程
            lock (threadLocker)
            {
                waitingForSpeak = true;
            }

            string newMessage = null;
            var startTime = DateTime.Now;

            // 启动语音合成，合成开始后返回。
            using (var result = synthesizer.StartSpeakingTextAsync(input).Result)
            {
                // Unity上还不支持本地播放（目前只支持Windows/Linux桌面）。
                // 在这里使用Unity API来播放音频作为短期解决方案。
                // 本机播放支持将在未来的版本中加入。

                var audioDataStream = AudioDataStream.FromResult(result);

                var isFirstAudioChunk = true;


                // 创建一个音频剪辑
                var audioClip = AudioClip.Create(
                    "Speech",
                    SampleRate * 600, // 最多可以讲10分钟的音频
                    1,
                    SampleRate,
                    true,
                    (float[] audioChunk) =>
                    {
                        var chunkSize = audioChunk.Length;
                        var audioChunkBytes = new byte[chunkSize * 2];
                        var readBytes = audioDataStream.ReadData(audioChunkBytes);
                        if (isFirstAudioChunk && readBytes > 0)
                        {
                            var endTime = DateTime.Now;
                            var latency = endTime.Subtract(startTime).TotalMilliseconds;
                            newMessage = $"Speech synthesis succeeded!\nLatency: {latency} ms.";
                            isFirstAudioChunk = false;
                        }

                        for (int i = 0; i < chunkSize; ++i)
                        {
                            if (i < readBytes / 2)
                            {
                                audioChunk[i] = (short)(audioChunkBytes[i * 2 + 1] << 8 | audioChunkBytes[i * 2]) /
                                                32768.0F;
                            }
                            else
                            {
                                audioChunk[i] = 0.0f;
                            }
                        }

                        if (readBytes == 0)
                        {
                            Thread.Sleep(200); //留出一些时间让audioSource完成播放。
                            audioSourceNeedStop = true;
                        }
                    });


                audioSource.clip = audioClip;

                // 由于需要获取语音合成的时长，所以需要等待语音合成结束后获得duration，再播放音频
                StartCoroutine(WaitUntilTrue());
            }

            lock (threadLocker)
            {
                if (newMessage != null)
                {
                    message = newMessage;
                }

                waitingForSpeak = false;
            }
        }

        //用于播放嘴部动画的协程
        private IEnumerator AnimateValue(float duration)
        {
            float startTime = Time.time;
            float endTime = startTime + duration + timeOffset;

            while (Time.time < endTime)
            {
                float timeSinceStart = Time.time - startTime;
                float range = animationRange;
                float speed = animationSpeed;

                float value = Mathf.PingPong(timeSinceStart * speed, range) + 1f;
                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, value);
                yield return null;
            }

            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, 0f);
        }

        private IEnumerator WaitUntilTrue()
        {
            while (!isDurationSet)
            {
                yield return null;
            }

            audioSource.Play();
            StartCoroutine(AnimateValue(audioDuration));
            this.isDurationSet = false;
        }

        // 下拉栏执行方法
        public void OnDropdownValueChanged(int value)
        {
            if (ChangeLanguageDropdown.options[value].text == "中文")
            {
                // 修改synthesizer是必须先销毁之前的synthesizer，再重新创建一个新的synthesizer
                synthesizer = null;

                soundSetting = "zh-CN-YunfengNeural";
                speechConfig.SpeechSynthesisVoiceName = soundSetting;
                synthesizer = new SpeechSynthesizer(speechConfig, null);

                CreatSynthesizer();
            }

            if (ChangeLanguageDropdown.options[value].text == "Espanol")
            {
                // 修改synthesizer是必须先销毁之前的synthesizer，再重新创建一个新的synthesizer
                synthesizer = null;

                soundSetting = "es-MX-GerardoNeural";
                speechConfig.SpeechSynthesisVoiceName = soundSetting;
                synthesizer = new SpeechSynthesizer(speechConfig, null);

                CreatSynthesizer();
            }

            if (ChangeLanguageDropdown.options[value].text == "English")
            {
                // 修改synthesizer是必须先销毁之前的synthesizer，再重新创建一个新的synthesizer
                synthesizer = null;

                soundSetting = "en-US-TonyNeural";
                speechConfig.SpeechSynthesisVoiceName = soundSetting;
                synthesizer = new SpeechSynthesizer(speechConfig, null);

                CreatSynthesizer();
            }
        }
    }
}