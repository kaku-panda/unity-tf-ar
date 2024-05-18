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

using System;

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class ThrowingObject : MonoBehaviour
{
    public Rigidbody rigidbody3D;
    private Collider[] colliders3D;

    public MaterialControl materialControl;

    [Header("Custom Data")]
    [Tooltip("Custom Flag for Any User Logic")]
    public bool flagCustom = false;

    [Tooltip("Convenient Access to Custom Control Script " +
        "of this Throwing Object")]
    public MonoBehaviour monoBehaviourCustom;

    [Header("Force")]
    [SerializeField]
    private float forceFactor;
    private Vector3 forceDirection;
    private Vector2 strength;
    private Vector2 strengthFactor;

    [SerializeField]
    private CameraAxes forceDirectionExtra = CameraAxes.TransformUp;
    private Vector3 forceDirectionExtraVector3;
    public enum CameraAxes
    {
        TransformUp,
        TransformForward,
        TransformRight,
        TransformUpRight,
        TransformLeft,
        TransformUpLeft
    }

    [Header("Torque")]
    public CameraAxes torqueAxis = CameraAxes.TransformRight;

    private Vector3 torqueAxisVector3;

    private float torqueAngleBasic;

    [SerializeField]
    private float torqueAngle;

    [SerializeField]
    private float torqueFactor;

    private Quaternion torqueRotation;

    [Tooltip("It clamps Torque")]
    [SerializeField]
    private float maxAngularVelocityAtAwake = 7f;

    [Header("Center Of Mass (Com)")]
    [Space]
    [SerializeField]
    private bool isComCustomizedAtAwake = false;

    [SerializeField]
    private Vector3 comCustom;

    [Space]
    [SerializeField]
    [Tooltip("If the Center of Mass by Default is not correct,"
        + "\nyou can use it as a base point for improving with Custom value")]
    private bool isComByDefaultLoggedAtAwake = false;

    [Space]
    [SerializeField]
    private bool isComCustomDrawnWithGizmo = false;

    [SerializeField]
    private float comCustomGizmoRadius = 0.08f;

#if UNITY_EDITOR

    [Space]
    [SerializeField]
    [Tooltip("To Customize Center Of Mass without Restart in FixedUpdate()")]
    private bool isComCustomizedAtFUpdateInEditor = false;

#endif

    private Quaternion rotationByDefault;
    public enum RotationsForNextThrow
    {
        Default,
        Random,
        Custom
    }

    [Header("Position")]
    [Tooltip("Middle is in the bottom of the screen: (0.5f, 0.1f)" +
        "\nY must be less Y of Input Position Fixed." +
        "\n\nLinked with Input Sensitivity.")]
    public Vector2 positionInViewportOnReset = new(0.5f, 0.1f);

    [Tooltip("Used for Z coordinate of Position On Reset")]
    public float cameraNearClipPlaneFactorOnReset = 7.5f;

    [Header("Rotation")]
    [SerializeField]
    private bool isObjectRotatedInThrowDirection = true;

    [SerializeField]
    private RotationsForNextThrow rotationOnReset =
        RotationsForNextThrow.Default;

    [SerializeField]
    private Vector3 rotationOnResetCustom = new(0f, 90f, 0f);

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioData[] audioDataCustom;

    [HideInInspector]
    public bool isThrown = false;

    private RigidbodyInterpolation interpolationByDefault;

    public event Action OnThrow;
    public event Action OnResetPhysicsBase;

    private void Awake()
    {
        rigidbody3D.maxAngularVelocity = maxAngularVelocityAtAwake;

        if (isComByDefaultLoggedAtAwake)
        {
            DebugPrinter.Print($"[Center Of Mass] by Default: {name}");
            DebugPrinter.Print($"Vector3(" +
                $"{rigidbody3D.centerOfMass.x}," +
                $"{rigidbody3D.centerOfMass.y}," +
                $"{rigidbody3D.centerOfMass.z})");
        }

        if (isComCustomizedAtAwake)
        {
            rigidbody3D.centerOfMass = comCustom;
        }

        colliders3D = GetComponentsInChildren<Collider>();

        rotationByDefault = rigidbody3D.rotation;

        interpolationByDefault = rigidbody3D.interpolation;

        if (!materialControl)
        {
            Debug.LogWarning(gameObject.name + " — materialControl is Null!");
        }
    }

#if UNITY_EDITOR

    private void FixedUpdate()
    {
        if (isComCustomizedAtFUpdateInEditor)
        {
            rigidbody3D.centerOfMass = comCustom;
        }
    }

#endif

    private void OnDrawGizmos()
    {
        if (isComCustomDrawnWithGizmo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(
                transform.position
                    + transform.rotation * comCustom,
                comCustomGizmoRadius);
        }
    }

    public void SetRendererEnabled(bool enabled)
    {
        if (materialControl)
        {
            materialControl.SetRendererEnabled(enabled);
        }
    }

    public void SetMaterial(Material material)
    {
        if (materialControl)
        {
            materialControl.SetMaterial(material);
        }
    }

    public void SetMaterial(int index)
    {
        if (materialControl)
        {
            materialControl.SetMaterial(index);
        }
    }

    public void ThrowBase(
        Vector2 inputPositionFirst,
        Vector2 inputPositionLast,
        Vector2 inputSensitivity,
        Transform cameraMain,
        int screenHight,
        float forceFactorExtra,
        float torqueFactorExtra,
        float torqueAngleExtra)
    {
        strengthFactor = inputPositionLast - inputPositionFirst;

        if (inputPositionLast.y < screenHight / 2
            && Mathf.Abs(strengthFactor.y) > 0f)
        {
            strengthFactor.x *= inputPositionLast.y / strengthFactor.y;

            //DebugPrinter.Print("[Correction] strengthFactor")
        }

        strengthFactor /= screenHight;

        strength.y = inputSensitivity.y * strengthFactor.y;
        strength.x = inputSensitivity.x * strengthFactor.x;

        forceDirection = new Vector3(strength.x, 0f, 1f);
        forceDirection =
            cameraMain.transform.TransformDirection(forceDirection);

        torqueAngleBasic = Mathf.Sign(strengthFactor.x)
            * Vector3.Angle(cameraMain.transform.forward, forceDirection);

        torqueRotation = Quaternion.AngleAxis(
            torqueAngleBasic + torqueAngle + torqueAngleExtra,
            cameraMain.transform.up);

        rigidbody3D.useGravity = true;
        rigidbody3D.interpolation = interpolationByDefault;

        forceDirectionExtraVector3 =
            GetCameraAxis(cameraMain, forceDirectionExtra);

        rigidbody3D.AddForce(
            (forceFactor + forceFactorExtra)
            * strength.y
            * (forceDirection + forceDirectionExtraVector3));

        if (isObjectRotatedInThrowDirection)
        {
            rigidbody3D.rotation =
                Quaternion.AngleAxis(
                    Mathf.Sign(strengthFactor.x) * Vector3.Angle(
                        cameraMain.transform.forward, forceDirection),
                    cameraMain.transform.up)
                * rigidbody3D.rotation;
        }

        torqueAxisVector3 = GetCameraAxis(cameraMain, torqueAxis);

        rigidbody3D.AddTorque(torqueRotation * torqueAxisVector3
            * (torqueFactor + torqueFactorExtra));

        OnThrow?.Invoke();
    }

    private Vector3 GetCameraAxis(Transform cameraMain, CameraAxes cameraAxis)
    {
        return cameraAxis switch
        {
            CameraAxes.TransformUp => cameraMain.transform.up,

            CameraAxes.TransformForward => cameraMain.transform.forward,

            CameraAxes.TransformRight => cameraMain.transform.right,

            CameraAxes.TransformUpRight =>
                cameraMain.transform.right + cameraMain.transform.up,

            CameraAxes.TransformLeft => cameraMain.transform.right * -1f,

            CameraAxes.TransformUpLeft =>
                cameraMain.transform.right * -1f + cameraMain.transform.up,

            _ => Vector3.zero
        };
    }

    public void ResetPhysicsBase()
    {
        //Debug.Log("ResetPhysics()");

        rigidbody3D.useGravity = false;
        rigidbody3D.velocity = Vector3.zero;
        rigidbody3D.angularVelocity = Vector3.zero;
        rigidbody3D.interpolation = RigidbodyInterpolation.None;

        OnResetPhysicsBase?.Invoke();
    }

    public void ResetPosition(Camera cameraMain)
    {
        Vector3 positionTargetTemp =
            cameraMain.ViewportToWorldPoint(new Vector3(
                positionInViewportOnReset.x,
                positionInViewportOnReset.y,
                cameraMain.nearClipPlane * cameraNearClipPlaneFactorOnReset));

        transform.position = positionTargetTemp;
    }

    public void ResetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    public void ResetRotation(Transform parent)
    {
        //DebugPrinter.Print(rotationByDefault.eulerAngles);

        Quaternion rotationTargetTemp;

        switch (rotationOnReset)
        {
            case RotationsForNextThrow.Default:
            default:

                if (parent)
                {
                    rotationTargetTemp = parent.rotation * rotationByDefault;
                }
                else
                {
                    rotationTargetTemp = rotationByDefault;
                }

                break;

            case RotationsForNextThrow.Random:

                rotationTargetTemp = GetRandomRotation();

                break;

            case RotationsForNextThrow.Custom:

                if (parent)
                {
                    rotationTargetTemp = parent.rotation
                        * Quaternion.Euler(rotationOnResetCustom);
                }
                else
                {
                    rotationTargetTemp =
                        Quaternion.Euler(rotationOnResetCustom);
                }

                break;
        }

        transform.rotation = rotationTargetTemp;
    }

    private Quaternion GetRandomRotation()
    {
        Quaternion randomRotation = new()
        {
            eulerAngles = new Vector3(
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 360f))
        };

        return randomRotation;
    }

    public void PlayAudioWhoosh()
    {
        PlayAudioRandomlyDependingOnSpeed(0, true);
    }

    public void PlayAudioRandomlyDependingOnSpeed(
        int index,
        bool isStoppedBeforePlay,
        AudioSource audioSource = null)
    {
        if (index >= 0
            && index < audioDataCustom.Length
            && audioDataCustom[index] != null)
        {
            if (audioSource)
            {
                PlayAudioRandomlyDependingOnSpeed(
                    audioDataCustom[index], isStoppedBeforePlay, audioSource);
            }
            else
            {
                PlayAudioRandomlyDependingOnSpeed(
                    audioDataCustom[index], isStoppedBeforePlay);
            }
        }
        else
        {
            DebugPrinter.Print("Audio Data doesn't exist at index: " + index);
        }
    }

    public void PlayAudioRandomlyDependingOnSpeed(
        AudioData audioData,
        bool isStoppedBeforePlay)
    {
        PlayAudioRandomlyDependingOnSpeed(
            audioData, isStoppedBeforePlay, audioSource);
    }

    public void PlayAudioRandomlyDependingOnSpeed(
        AudioData audioData, bool isStoppedBeforePlay, AudioSource audioSource)
    {
        float speedClamp = Mathf.Clamp(
            rigidbody3D.velocity.magnitude,
            audioData.speedClampMin,
            audioData.speedClampMax);

        audioSource.pitch =
            audioData.pitchMin + speedClamp * audioData.pitchFactor;

        if (isStoppedBeforePlay)
        {
            audioSource.Stop();
        }

        audioSource.PlayOneShot(
            audioData.audioClips[
                UnityEngine.Random.Range(0, audioData.audioClips.Length)],
            speedClamp * audioData.volumeFactor);

        //DebugPrinter.Print(
        //    $"AudioSource: {audioSource.name}, Volume: {audioSource.volume}");
    }

    public void ActivateTriggersOnColliders(bool isTrigger)
    {
        for (int i = 0; i < colliders3D.Length; i++)
        {
            colliders3D[i].isTrigger = isTrigger;
        }
    }

    public void SetCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < colliders3D.Length; i++)
        {
            colliders3D[i].enabled = enabled;
        }
    }

    [System.Serializable]
    public class AudioData
    {
        public string nameForLog;

        public AudioClip[] audioClips;

        public float speedClampMin = 0f;
        public float speedClampMax = 15f;

        [Range(-3f, 3f)]
        public float pitchMin = 0.8f;
        public float pitchFactor = 0.02f;

        public float volumeFactor = 0.125f;

        public AudioData() { }

        public AudioData(
            string nameForLog,
            AudioClip[] audioClips,
            float speedClampMin = 0f,
            float speedClampMax = 15f,
            float pitchMin = 0.8f,
            float pitchFactor = 0.02f,
            float volumeFactor = 0.125f)
        {
            this.nameForLog = nameForLog;
            this.audioClips = audioClips;
            this.speedClampMin = speedClampMin;
            this.speedClampMax = speedClampMax;
            this.pitchMin = pitchMin;
            this.pitchFactor = pitchFactor;
            this.volumeFactor = volumeFactor;
        }
    }
}