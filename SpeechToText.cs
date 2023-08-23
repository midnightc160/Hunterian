//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
// <code>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using TMPro;
using Utilities.Async.AwaitYieldInstructions;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

#if PLATFORM_IOS
using UnityEngine.iOS;
using System.Collections;
#endif


public class SpeechToText : MonoBehaviour
{
    public Button RecordButton;

    private object threadLocker = new object();
    private bool waitingForReco;
    private string message;

    private bool micPermissionGranted = false;

    // 设置下拉UI用于切换文本识别语言
    [SerializeField] private TMP_Dropdown ChangeLanguageDropdown;
    [SerializeField] private string subscriptionKey = "ID";
    [SerializeField] private string region = "region";
    private SpeechConfig speechConfig;


#if PLATFORM_ANDROID || PLATFORM_IOS
    // Required to manifest microphone permission, cf.
    // https://docs.unity3d.com/Manual/android-manifest.html
    private Microphone mic;
#endif
#if ENABLE_WINMD_SUPPORT
    string logFile = Application.persistentDataPath + "/logFile.txt";
    speechConfig.SetProperty(PropertyId.Speech_LogFilename, logFile);
#endif

    public TMP_InputField inputField;


    void Start()
    {
        // Creates an instance of a speech config with specified subscription key and service region.
        // Replace with your own subscription key and service region (e.g., "westus").
        speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);

        speechConfig.SpeechRecognitionLanguage = "en-GB";
        speechConfig.SetProperty(PropertyId.Speech_LogFilename, "LogfilePathAndName");

        ChangeLanguageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        // 分割线 --------------------------------------------------------------------------------------------

        if (RecordButton == null)
        {
            message = "startRecoButton property is null! Assign a UI Button to it.";
            UnityEngine.Debug.LogError(message);
        }
        else
        {
            // Continue with normal initialization, Text and Button objects are present.
#if PLATFORM_ANDROID
            // Request to use the microphone, cf.
            // https://docs.unity3d.com/Manual/android-RequestingPermissions.html
            message = "Waiting for mic permission";
            Debug.Log(message);
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#elif PLATFORM_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
#else
            micPermissionGranted = true;
            message = "Click button to recognize speech";
            Debug.Log(message);
#endif
            RecordButton.onClick.AddListener(ButtonClick);
        }
    }

    void Update()
    {
#if PLATFORM_ANDROID
        if (!micPermissionGranted && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micPermissionGranted = true;
            message = "Click button to recognize speech";
             Debug.Log(message);
        }
#elif PLATFORM_IOS
        if (!micPermissionGranted && Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            micPermissionGranted = true;
            message = "Click button to recognize speech";
        }
#endif


        lock (threadLocker)
        {
            if (RecordButton != null)
            {
                RecordButton.interactable = !waitingForReco && micPermissionGranted;
            }
        }
    }

    public async void ButtonClick()
    {
        // Make sure to dispose the recognizer after use!
        using (var recognizer = new SpeechRecognizer(speechConfig))
        {
            lock (threadLocker)
            {
                waitingForReco = true;
            }

            // Starts speech recognition, and returns after a single utterance is recognized. The end of a
            // single utterance is determined by listening for silence at the end or until a maximum of 15
            // seconds of audio is processed.  The task returns the recognition text as result.
            // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
            // shot recognition like command or query.
            // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
            var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
            
            // Checks result.
            string newMessage = string.Empty;
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                newMessage = result.Text;

                // 在别的线程更新输入框的值，都会导致输入栏不显示更新后的值，必须点击后才可以显示，所以使用了一个插件实现在主线程更新输入框的值
                UnityMainThreadDispatcher.Instance().Enqueue(() => { inputField.text += newMessage; });
                //inputField.text += newMessage;
                Debug.Log(newMessage);
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                newMessage = "NOMATCH: Speech could not be recognized.";
                Debug.Log(newMessage);
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                newMessage = $"CANCELED: Reason={cancellation.Reason} ErrorDetails={cancellation.ErrorDetails}";
                Debug.Log(newMessage);
            }

            lock (threadLocker)
            {
                message = newMessage;
                waitingForReco = false;
            }
        }
    }

    // 下拉栏执行方法
    public void OnDropdownValueChanged(int value)
    {
        if (ChangeLanguageDropdown.options[value].text == "中文")
        {
            speechConfig.SpeechRecognitionLanguage = "zh-CN";
        }

        if (ChangeLanguageDropdown.options[value].text == "Espanol")
        {
            speechConfig.SpeechRecognitionLanguage = "es-MX";
        }

        if (ChangeLanguageDropdown.options[value].text == "English")
        {
            speechConfig.SpeechRecognitionLanguage = "en-GB";
        }
    }
}