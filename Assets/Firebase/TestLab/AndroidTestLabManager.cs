// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if UNITY_ANDROID

using System;
using UnityEngine;

namespace Firebase.TestLab {
  internal class AndroidTestLabManager : TestLabManager {
    public AndroidTestLabManager() {
      Debug.Log("Getting Unity Player class");
      // get the UnityPlayerClass
      AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

      Debug.Log("Getting current Activity");
      // get the current activity
      _activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

      Debug.Log("Getting Activity's intent");
      // get the intent from the activity
      _intent = _activity.Call<AndroidJavaObject>("getIntent");

      CheckIntentForScenario();

      if (IsTestingScenario) {
        Debug.Log("Initializing logging");
        InitializeLogging();
      }

      Debug.Log("All done!");
    }

    public override void NotifyHarnessTestIsComplete() {
      _activity.Call("finish");
    }

    public override void LogToResults(string s) {
      LibcWrapper.WriteToFileDescriptor(_logFileDescriptor, s);
    }

    private readonly AndroidJavaObject _activity;
    private readonly AndroidJavaObject _intent;

    private int _logFileDescriptor = -1;

    private void CheckIntentForScenario() {
      Debug.Log("Getting Intent's action");
      string action = _intent.Call<string>("getAction");

      Debug.Log("Checking for test loop");
      if (action != "com.google.intent.action.TEST_LOOP") return;

      ScenarioNumber = _intent.Call<int>("getIntExtra", "scenario", NoScenarioPresent);

      Debug.Log("Fetched scenario: " + ScenarioNumber);
    }

    private void InitializeLogging() {
      // get the data from the intent, which is a Uri
      AndroidJavaObject logFileUri = _intent.Call<AndroidJavaObject>("getData");
      if (logFileUri == null) return;

      string encodedPath = logFileUri.Call<string>("getEncodedPath");
      Debug.Log("[FTL] Log file is located at: " + encodedPath);

      try {
        int fd = _activity
          .Call<AndroidJavaObject>("getContentResolver")
          .Call<AndroidJavaObject>("openAssetFileDescriptor", logFileUri, "w")
          .Call<AndroidJavaObject>("getParcelFileDescriptor")
          .Call<int>("getFd");
        Debug.Log("[FTL] extracted file descriptor from intent: " + fd);
        _logFileDescriptor = LibcWrapper.dup(fd);
        Debug.Log("[FTL] duplicated file descriptor: " + _logFileDescriptor);
        Debug.Log("[FTL] trying to write to fd: " + _logFileDescriptor);
      }
      catch (Exception e) {
        Debug.LogError("[FTL] - exception fetching log file descriptor: "
                       + e
                       + "\n"
                       + e.StackTrace);
      }
    }
  }
}

#endif // UNITY_ANDROID