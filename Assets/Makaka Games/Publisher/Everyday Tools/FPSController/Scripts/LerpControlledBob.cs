/*
===================================================================
Unity Assets by MAKAKA GAMES: https://makaka.org/o/all-unity-assets
===================================================================

Online Docs (Latest): https://makaka.org/unity-assets
Offline Docs: You have a PDF file in the package folder.

=======
SUPPORT
=======

First of all, read the docs. If it didn’t help, get the support.

Web: https://makaka.org/support
Email: info@makaka.org

If you find a bug or you can’t use the asset as you need, 
please first send email to info@makaka.org
before leaving a review to the asset store.

I am here to help you and to improve my products for the best.
*/

using System;
using System.Collections;
using UnityEngine;

namespace UnityStandardAssets.Utility
{
    [Serializable]
    public class LerpControlledBob
    {
        public float BobDuration;
        public float BobAmount;

        private float m_Offset = 0f;

        // provides the offset that can be used
        public float Offset()
        {
            return m_Offset;
        }

        public IEnumerator DoBobCycle()
        {
            // make the camera move down slightly
            float t = 0f;

            while (t < BobDuration)
            {
                m_Offset = Mathf.Lerp(0f, BobAmount, t / BobDuration);

                t += Time.deltaTime;

                yield return new WaitForFixedUpdate();
            }

            // make it move back to neutral
            t = 0f;

            while (t < BobDuration)
            {
                m_Offset = Mathf.Lerp(BobAmount, 0f, t / BobDuration);

                t += Time.deltaTime;

                yield return new WaitForFixedUpdate();
            }

            m_Offset = 0f;
        }
    }
}
