using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Utils
{
    public static class Awaiters
    {
        public static async Task WaitUntil(Func<bool> predicate, CancellationToken cancellationToken = default)
        {
            while (!predicate.Invoke())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
        
        public static async Task WaitForSeconds(float seconds, CancellationToken cancellationToken = default)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // When the editor is not playing scaled time is the same as realtime
                await WaitForSecondsRealtime(seconds, cancellationToken);
                return;
            }
#endif
            var endTime = Time.time + seconds;
            while (Time.time < endTime)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
        
        private static async Task WaitForSecondsRealtime(float seconds, CancellationToken cancellationToken = default)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var editorEndTime = (float)EditorApplication.timeSinceStartup + seconds;
                while ((float)EditorApplication.timeSinceStartup < editorEndTime)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                return;
            }
#endif
            var endTime = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < endTime)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
    }
}