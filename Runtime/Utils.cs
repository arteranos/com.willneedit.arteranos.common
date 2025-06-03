/*
 * Copyright (c) 2025, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Arteranos.Common
{
    public static class Utils
    {
        /// <summary>
        /// Copy from inStream to outStream, report its progress.
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="outStream"></param>
        /// <param name="reportProgress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task CopyWithProgress(Stream inStream, Stream outStream, Action<long> reportProgress = null, CancellationToken token = default)
        {
            long totalBytes = 0;
            long lastreported = 0;
            // 0.5MB. Should be a compromise between of too few progress reports and bandwidth bottlenecking
            byte[] buffer = new byte[512 * 1024];

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await inStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);

                if (bytesRead == 0) break;

                totalBytes += bytesRead;
                if (totalBytes >= lastreported + 512 * 1024)
                {
                    reportProgress?.Invoke(totalBytes);
                    lastreported = totalBytes;
                }

                await outStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
            }
            outStream.Flush();
            outStream.Close();
        }

        /// <summary>
        /// Take a screenshot on a camera with a Render Texture
        /// </summary>
        /// <param name="cam">Camera, positioned and with a valid Render Texture</param>
        /// <param name="stream">Output stream to write the PNG to</param>
        /// <returns>Coroutine IEnumerator</returns>
        public static IEnumerator TakePhoto(Camera cam, Stream stream)
        {
            RenderTexture rt = cam.targetTexture;

            RenderTexture mRt = new(rt.width, rt.height, rt.depth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = rt.antiAliasing
            };

            Texture2D tex = new(rt.width, rt.height, TextureFormat.ARGB32, false);
            cam.targetTexture = mRt;
            cam.Render();
            RenderTexture.active = mRt;

            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            cam.targetTexture = rt;
            RenderTexture.active = rt;

            byte[] Bytes = tex.EncodeToPNG();
            Task t = stream.WriteAsync(Bytes, 0, Bytes.Length);

            yield return new WaitUntil(() => t.IsCompleted);

            Object.Destroy(tex);

            Object.Destroy(mRt);

            yield return null;
        }

        /// <summary>
        /// Get the directory name (and its root filename) for the
        /// appropiate AssetBundle for the current architecture
        /// </summary>
        /// <returns>The name</returns>
        public static string GetArchitectureDirName()
        {
            RuntimePlatform p = Application.platform;
            return GetArchitectureDirName(p);
        }

        /// <summary>
        /// Get the directory name (and its root filename) for the
        /// appropiate AssetBundle
        /// </summary>
        /// <param name="p">The Runtime platform</param>
        /// <returns>The name</returns>
        public static string GetArchitectureDirName(RuntimePlatform p) => p switch
        {
            RuntimePlatform.OSXEditor or
            RuntimePlatform.OSXPlayer or
            RuntimePlatform.OSXServer => "Mac",
            RuntimePlatform.LinuxEditor or
            RuntimePlatform.LinuxPlayer or
            RuntimePlatform.LinuxServer => "Linux",
            RuntimePlatform.Android => "Android",
            _ => "Windows",
        };

 
    }
}