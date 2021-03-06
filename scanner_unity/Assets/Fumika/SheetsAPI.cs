﻿using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Fumika {
    public class APIResponse {
        readonly public long ResponseCode;

        readonly public string Error;
        readonly public bool IsError;
        readonly JSONObject ResponseJSON;

        public APIResponse(UnityWebRequest www) {
            IsError = www.isError;

            if (IsError == false) {
                ResponseCode = www.responseCode;
                Error = www.error;

                var text = www.downloadHandler.text;
                ResponseJSON = new JSONObject(text, int.MaxValue);
                Debug.Log(text);
            }
        }
    }

    public class SheetsAPI : MonoBehaviour {
        public static SheetsAPI Instance { get; private set; }

        public string sheetName = "ISBN List";
        public string SheetID = "13IXCsO7FPjhUmOG0bp08xfZXlusdkSER6b33xPMvV9M";

        [SerializeField]
        string clientId = "488206440345-qoil6mlkij2ka8bh5fd8nibdar8lpjdg.apps.googleusercontent.com";

        [SerializeField]
        string clientSecret = "paj_17vxQbCJcg3kKv8omqA4";

        [SerializeField]
        TokenContainer storage = null;

        GoogleDrive drive;

        public APIResponse LastResponse { get; private set; }

        public IEnumerator BeginAppendValue(string val) {
            var range = "A1";
            var uri = string.Format("https://sheets.googleapis.com/v4/spreadsheets/{0}/values/{1}:append", SheetID, range);

            // Query parameters
            var qs = new QueryStringBuilder();
            qs.Add("valueInputOption", "USER_ENTERED");
            qs.Add("insertDataOption", "INSERT_ROWS");
            qs.Add("includeValuesInResponse", "true");
            qs.Add("responseValueRenderOption", "FORMULA");
            qs.Add("responseDateTimeRenderOption", "FORMATTED_STRING");
            var querystring = qs.ToString();

            JSONObject row = new JSONObject(JSONObject.Type.ARRAY);
            row.Add(val);

            JSONObject values = new JSONObject(JSONObject.Type.ARRAY);
            values.Add(row);

            JSONObject requestJSON = new JSONObject();
            requestJSON.AddField("values", values);

            var url = uri + "?" + querystring;
            UnityWebRequest www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

            www.SetRequestHeader("Authorization", "Bearer " + storage.AccessToken);

            byte[] bytes = Encoding.UTF8.GetBytes(requestJSON.ToString());
            UploadHandlerRaw uH = new UploadHandlerRaw(bytes);
            uH.contentType = "application/json";
            www.uploadHandler = uH;

            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.Send();

            while(!www.isDone) {
                yield return null;
            }

            var resp = new APIResponse(www);
            LastResponse = resp;
        }

        bool initInProgress = false;

        public IEnumerator BeginInit() {
            initInProgress = true;

            var scopes = new string[] {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/drive.appdata",

                "https://www.googleapis.com/auth/drive",
                "https://www.googleapis.com/auth/spreadsheets",
            };

            drive = new GoogleDrive(storage);
            drive.ClientID = clientId;
            drive.ClientSecret = clientSecret;
            drive.Scopes = scopes;

            var authorization = drive.Authorize();
            yield return StartCoroutine(authorization);

            if (authorization.Current is GoogleDrive.Exception) {
                Debug.LogWarning(authorization.Current as GoogleDrive.Exception);
                initInProgress = false;
                yield break;

            } else {
                Debug.Log("User Account: " + drive.UserAccount);
            }

            // 윈도우에서는 되는데 안드로이드에서는 안되서 하드코딩
            /*
            var finder = new SheetsAPI_FindSheet(drive, this);
            yield return finder.BeginFindSheet(sheetName);
            if (!finder.Found) {
                Debug.LogWarningFormat("Cannot find Sheet, {0}", sheetName);
            }
            SheetID = finder.SheetID;
            */

            initInProgress = false;
        }

        public IEnumerator BeginWaitInitialize() {
            while(drive == null) {
                yield return null;
            }
            while (initInProgress == true) {
                yield return null;
            }
        }

        void Awake() {
            Debug.Assert(Instance == null);
            Instance = this;
        }

        void Start() {
            StartCoroutine(BeginInit());
        }

        void OnDestroy() {
            Debug.Assert(Instance == this);
            Instance = null;
        }

        bool revokeInProgress = false;
        public IEnumerator BeginRevoke() {
            revokeInProgress = true;
            yield return StartCoroutine(drive.Unauthorize());
            revokeInProgress = false;
        }
    }
}