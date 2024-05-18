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

using UnityEngine;

using System.Collections;

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class MaterialControl : MonoBehaviour
{
    [SerializeField]
    private Renderer renderer3D;

    [SerializeField]
    private MaterialData[] materialDataCustom;

    private Material materialStandardShared;

    // To avoid Material Clones
    private Material materialStandardClone;

    [Header("Shader Parameter Changing")]
    [SerializeField]
    private bool isParameterChangingOnAtStart = true;

    [SerializeField]
    private string parameter = "_Cutoff";

    [SerializeField]
    [Tooltip("1 = Fading In On Start.")]
    private float parameterOnStart = 0f;

    [SerializeField]
    private float parameterMax = 1f;

    [SerializeField]
    private float parameterMin = 0f;

    private float parameterCurrent = 0f;

    [Header("Delays (Parameter Changing must be completed before" +
        " Object's Deactivating)")]
    public float delayOut = 0f;
    public float delayIn = 0f;

    [Header("Speed (Parameter = Time)")]
    [SerializeField]
    private AnimationCurve speedOut =
        AnimationCurve.Linear(0f, 0.02f, 1f, 0.02f);

    [SerializeField]
    private float speedOutFactor = 1f;

    [Space]
    [SerializeField]
    private AnimationCurve speedIn =
        AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [SerializeField]
    private float speedInFactor = 1f;

    private void Awake()
    {
        //MaterialCounter.Print();

        materialStandardShared = renderer3D.sharedMaterial;

        //MaterialCounter.Print();
    }

    private void Start()
    {
        if (isParameterChangingOnAtStart)
        {
            materialStandardClone = new Material(materialStandardShared);

            parameterCurrent = parameterOnStart;

            Step();

            //MaterialCounter.Print();
        }
    }

    public void Fade(
        bool isFadeIn,
        bool isResetFadingInTheEnd,
        bool isResetColorToStandardInTheBeginning = false)
    {
        StartCoroutine(FadeCoroutine(
            isFadeIn,
            isResetFadingInTheEnd,
            isResetColorToStandardInTheBeginning));
    }

    public IEnumerator FadeCoroutine(
        bool isFadeIn,
        bool isResetFadingInTheEnd,
        bool isResetColorToStandardInTheBeginning = false)
    {
        if (isParameterChangingOnAtStart)
        {
            yield return new WaitForSeconds(isFadeIn ? delayIn : delayOut);

            //MaterialCounter.Print();

            if (isResetColorToStandardInTheBeginning)
            {
                materialStandardClone.color = materialStandardShared.color;
            }

            if (isFadeIn)
            {
                //DebugPrinter.Print("is Fade In");

                while (parameterCurrent > parameterMin)
                {
                    parameterCurrent -=
                        speedIn.Evaluate(parameterCurrent) * speedInFactor;

                    Step();

                    yield return new WaitForFixedUpdate();
                }
            }
            else
            {
                //DebugPrinter.Print("is Fade Out");

                parameterCurrent = parameterMin;

                while (parameterCurrent < parameterMax)
                {
                    parameterCurrent +=
                        speedOut.Evaluate(parameterCurrent) * speedOutFactor;

                    Step();

                    yield return new WaitForFixedUpdate();
                }

                //DebugPrinter.Print("Finish Fade Out");
            }

            if (isResetFadingInTheEnd)
            {
                ResetFade();
            }

            //MaterialCounter.Print();

            //DebugPrinter.Print("Finish of Fading.");
        }
    }

    private void Step()
    {
        if (renderer3D.sharedMaterial != materialStandardClone)
        {
            // if (gameObject.name == "Net")
            // {
            //     DebugPrinter.Print(materialStandardClone.shader.name);

            //     DebugPrinter.Print(renderer3D.sharedMaterial.shader.name);
            // }

            materialStandardClone.color = renderer3D.sharedMaterial.color;

            SetMaterial(materialStandardClone);
        }

        materialStandardClone.SetFloat(parameter, parameterCurrent);

        //DebugPrinter.Print("alphaCurrent = " + alphaCurrent);
    }

    private void ResetFade()
    {
        if (isParameterChangingOnAtStart)
        {
            parameterCurrent = parameterMin;

            SetMaterialStandard();
        }
    }

    public void SetMaterial(Material material)
    {
        renderer3D.material = material;
    }

    public void SetMaterial(int index)
    {
        if (index >= 0
            && index < materialDataCustom.Length
            && materialDataCustom[index] != null)
        {
            renderer3D.material = materialDataCustom[index].material;
        }
        else
        {
            DebugPrinter.Print("Material doesn't exist at index: " + index);
        }
    }

    private void SetMaterialStandard()
    {
        renderer3D.material = materialStandardShared;
    }

    public void SetRendererEnabled(bool enabled)
    {
        if (renderer3D)
        {
            renderer3D.enabled = enabled;
        }
        else
        {
            DebugPrinter.Print("Renderer is Null!");
        }
    }

    [System.Serializable]
    public class MaterialData
    {
        public string nameForLog;

        public Material material;

        public MaterialData() { }

        public MaterialData(
            string nameForLog,
            Material material)
        {
            this.nameForLog = nameForLog;
            this.material = material;
        }
    }
}
