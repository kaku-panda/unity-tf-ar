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

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

#pragma warning disable 649

/// <summary>
/// Control script to operate with all throwing objects.
/// </summary>
/// <remarks>
/// Unity Events are not effective. 
/// I use them to show you clearly in Unity Editor,
/// where you can insert your code.
/// If you want more performance, you need to use C# Events.
/// <para />
/// Because of coroutines behavior, it is impossible to place some methods
/// inside ThrowingObject class. OOP does not work in this case,
/// but we have a stable throwing with Object Pool.
/// </remarks>
[HelpURL("https://makaka.org/unity-assets")]
public class ThrowControl : MonoBehaviour
{
	[SerializeField]
	private RandomObjectPooler randomObjectPooler;

	[Space]
	public UnityEvent OnInitialized;

	[Header("FPS (throw force takes into account the speed" +
		" of the player's movement) ")]
	public CharacterController characterControllerFPS;
	private float characterControllerFPSSpeedCurrent = 0f;

	[Header("Camera")]
	public Camera cameraMain;

	[Header("Modes")]
	public Mode modeAtAwake = Mode.ClickOrTap;
    public Mode modeAtAwakeForMobile = Mode.ClickOrTap;

    public enum Mode
	{
		Flick,
		ClickOrTap,
		PressKey
	}

    private bool isWebGLOnMobile = false;

    [Header("Press Key Mode")]
    [SerializeField]
    private KeyCode KeyForPressKey;

    [Tooltip("If it's false then it allows fast flicks only." +
		"\n\nPositions in the last & previous frames are taken into account." +
		"\n\nPlay with params: Force Factor Extra (45)" +
		" & Input Sensivity X (7)")]
    [Header("Flick Mode")]
    public bool isFullPathForFlick = true;

	public float lerpTimeFactorOnTouchForFlick = 20f;

	private bool isTouchForFlick = false;

	[Header("Throw")]
	public bool isThrowingBlockedWhenClickingUI = true;

	[Tooltip("Actual for FPS Controller")]
	public bool isInputPositionFixed = false;

	[Range(0.01f, 1f)]
	public float inputPositionFixedScreenFactorX = 0.48f;

	[Range(0.01f, 1f)]
	public float inputPositionFixedScreenFactorY = 0.52f;
	public Vector2 inputSensitivity = new(1f, 100f);

	public float forceFactorExtra = 10f;
	public float torqueFactorExtra = 60f;
	public float torqueAngleExtra;

	public Transform parentOnThrow;

	[Space]
	public UnityEventWithThrowingObject OnThrow;

	[Header("Next Throw")]
	[Range(0.1f, 10f)]
	public float nextThrowGettingDelay = 0.1f;

	/// <summary> Seconds for next try of coroutine call (min = 0.1f)</summary>
	private const float nextCoroutineCallTryDelay = 0.1f;
	private bool isNextThrowGetting;

	private GameObject gameObjectTemp;
	private ThrowingObject throwingObjectTemp;

	[Space]
	public UnityEventWithThrowingObject OnNextThrowGetting;

	[Header("Tag")]
	public bool isTagCustomSetOnInit = false;

	[TagSelector]
	public string tagCustomOnInit = TagSelectorAttribute.Untagged;

	[Header("Layer Changing (to neutralize mutual collisions)")]
	public bool isLayerCustomSetOnInit = true;

	[TagSelector]
	public LayerMask layerOnInit;

	[Space]
	public bool isLayerCustomSetOnThrowAndReset = true;

	[Range(0f, 5f)]
	public float layerSettingOnThrowDelay = 1f;

	public LayerMask layerOnThrow;
	public LayerMask layerOnReset;

	[Tooltip("May be useful in very rare cases. E.g., when you play with" +
		" Time Scale and Layers can't be changed quickly." +
		"\n\nNote: If you are dealing with Cloth, then you must operate" +
		" with it outside of this asset to avoid collisions when" +
		" OnReset() Event is dispatched:" +
		"\nnull Cloth Collider every dispatch of OnNextThrowGetting() Event" +
		" & register Cloth Collider every dispatch of OnThrow() Event.")]
	[Range(0f, 1f)]
	public float layerSettingOnResetFinishingDelay = 0f;

	private int layerIndexOnInit;
	private int layerIndexOnThrow;
	private int layerIndexOnReset;

	[Header("Reset (must be called after the end of Fading Out)")]
	[Range(0f, 10f)]
	public float resetDelay = 4f;

	[Space]
	public UnityEventWithThrowingObject OnReset;

	private GameObject gameObjectCurrent;
	private ThrowingObject throwingObjectCurrent;

	private RaycastHit raycastHit;

	private bool isInputBegan = false;
	private bool isInputEnded = false;
	private bool isInputHeldDown = false;

	private Vector3 inputPositionCurrent;
	private Vector3 inputPositionPivot;

	[Header("Fading")]
	public bool isFadingOn = true;

	[Header("Fading Out (must be completed before Reset)")]
	[Space]
	public UnityEventWithThrowingObject OnFadingOut;

	[System.Serializable]
	public class UnityEventWithThrowingObject : UnityEvent<ThrowingObject> { }

	// ---------
	// DEBUGGING
	// ---------

	// #if DEBUG
	// private NumberDebugger positionDebugger = new NumberDebugger();
	// #endif

	/// <summary>Call after pool initialisation.</summary>
	public void InitThrowingObjects()
	{
		StartCoroutine(InitThrowingObjectsCoroutine());
	}

	/// <summary>Init physics correctly.</summary>
	private IEnumerator InitThrowingObjectsCoroutine()
	{
		if (randomObjectPooler)
		{
			if (cameraMain)
			{
				InitLayerIndexes();

				randomObjectPooler.InitControlScripts(typeof(ThrowingObject));

				for (int i = 0; i < randomObjectPooler.pooledObjects.Count; i++)
				{
					gameObjectTemp = randomObjectPooler.pooledObjects[i];

					if (gameObjectTemp)
					{
						gameObjectTemp.SetActive(true);

						throwingObjectTemp =
							randomObjectPooler.RegisterControlScript(
								gameObjectTemp) as ThrowingObject;

						throwingObjectTemp.SetRendererEnabled(false);

						if (isTagCustomSetOnInit)
						{
							throwingObjectTemp.tag = tagCustomOnInit;
						}

						if (isLayerCustomSetOnInit)
						{
							ChangeLayer(throwingObjectTemp.gameObject,
								layerIndexOnInit);
						}

						yield return new WaitForFixedUpdate();

						StartCoroutine(ResetCoroutine
							(nextCoroutineCallTryDelay, throwingObjectTemp,
							true));

						yield return new WaitForFixedUpdate();
					}
				}

				yield return new WaitForSeconds(nextCoroutineCallTryDelay);

				yield return null;
				yield return null;
				yield return null;

				isWebGLOnMobile = Application.isMobilePlatform
					&& Application.platform == RuntimePlatform.WebGLPlayer;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL

                if (isWebGLOnMobile)
                {
                    modeAtAwake = modeAtAwakeForMobile;
                }

#elif UNITY_IOS || UNITY_ANDROID

				modeAtAwake = modeAtAwakeForMobile;

#else

				Debug.LogWarning("Indicate your platform here" +
					" for platform dependent compilation.");

#endif

                OnInitialized.Invoke();
			}
			else
			{
				Debug.LogError("Camera Main is Null. Assign it in the Editor.");
			}
		}
		else
		{
			Debug.LogError("Random Object Pooler is Null." +
				" Assign it in the Editor.");
		}
	}

	public void GetFirstThrow()
	{
		GetNextThrow(nextCoroutineCallTryDelay);
	}

	private void Update()
	{
		if (!isNextThrowGetting && gameObjectCurrent
			&& throwingObjectCurrent && !throwingObjectCurrent.isThrown)
		{

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL

			if (modeAtAwake == Mode.PressKey)
			{
				isInputBegan = Input.GetKeyDown(KeyForPressKey);
				isInputEnded = Input.GetKeyUp(KeyForPressKey);
				isInputHeldDown = Input.GetKey(KeyForPressKey);
            }
			else
			{
				isInputBegan = Input.GetMouseButtonDown(0);
				isInputEnded = Input.GetMouseButtonUp(0);
				isInputHeldDown = Input.GetMouseButton(0);
			}

            inputPositionCurrent =
                isInputPositionFixed
                ? GetInputPositionFixed()
                : Input.mousePosition;

#elif UNITY_IOS || UNITY_ANDROID

			if (Input.touchCount == 1)
			{
				isInputBegan = Input.GetTouch(0).phase == TouchPhase.Began;
				isInputEnded = Input.GetTouch(0).phase == TouchPhase.Ended;
				isInputHeldDown = true;

				inputPositionCurrent = 
					isInputPositionFixed 
					? GetInputPositionFixed() 
					: (Vector3)Input.GetTouch(0).position;
			}
			else
			{
				return;
			}

#else

			Debug.LogWarning("Indicate your platform here" +
                " for platform dependent compilation.");

#endif

            if (isTouchForFlick)
			{
				//DebugPrinter.Print("isTouchForFlick");

				StartCoroutine(OnTouchForFlickCoroutine());
			}

			//DebugPrinter.Print("isThrown check: " + isThrown);

			if (isInputBegan && !IsPointerOverUIObject(inputPositionCurrent))
			{
				//DebugPrinter.Print("isInputBegan");

				if (Physics.Raycast(cameraMain.ScreenPointToRay(
					inputPositionCurrent), out raycastHit, 100f))
				{
					if (modeAtAwake == Mode.Flick)
					{
						if (raycastHit.rigidbody
							== throwingObjectCurrent.rigidbody3D)
						{
							isTouchForFlick = true;

							inputPositionPivot = inputPositionCurrent;
						}
						// If click or tap OUTSIDE of Throwing Game Object
						// when Flick Mode => No Throw
						else
						{
							inputPositionPivot = Vector3.zero;
						}
					}
				}
				// If click or tap OUTSIDE Any Object
				else
				{
					if (modeAtAwake == Mode.Flick)
					{
						inputPositionPivot = Vector3.zero;
					}
				}
			}

			// Next Update()
			if (isInputEnded)
			{
				//DebugPrinter.Print("isInputEnded");

				if (modeAtAwake == Mode.Flick
					&& inputPositionPivot != Vector3.zero)
				{
					//DebugPrinter.Print("Mode.Flick => Throw()");

					//DebugPrinter.Print(inputPositionPivot);
					//DebugPrinter.Print(inputPositionCurrent);

					throwingObjectCurrent.isThrown = true;

					StartCoroutine(ThrowCoroutine(
						inputPositionPivot,
						inputPositionCurrent,
						throwingObjectCurrent));

					isTouchForFlick = false;

					inputPositionPivot = Vector3.zero;
				}
			}

			if (isInputHeldDown && !IsPointerOverUIObject(inputPositionCurrent))
			{
				//DebugPrinter.Print("isInputHeldDown");

				//It allows fast flicks only. See ToolTip for isFullPathForFlick
				if (modeAtAwake == Mode.Flick && !isFullPathForFlick)
				{
					if (inputPositionPivot != Vector3.zero)
					{
						inputPositionPivot = inputPositionCurrent;
					}
				}
				else if ((modeAtAwake == Mode.ClickOrTap
					|| modeAtAwake == Mode.PressKey)
                    && inputPositionPivot.y < inputPositionCurrent.y)
				{
					//DebugPrinter.Print("Mode.ClickOrTap => Throw()");

					inputPositionPivot = cameraMain.ViewportToScreenPoint(
						throwingObjectCurrent.positionInViewportOnReset);

					throwingObjectCurrent.isThrown = true;

					StartCoroutine(ThrowCoroutine(
						inputPositionPivot,
						inputPositionCurrent,
						throwingObjectCurrent));
				}
			}
		}
	}

	private Vector3 GetInputPositionFixed()
	{
		return new Vector3(
			Screen.width * inputPositionFixedScreenFactorX,
			Screen.height * inputPositionFixedScreenFactorY,
			0f);
	}

	private IEnumerator OnTouchForFlickCoroutine()
	{
		yield return new WaitForFixedUpdate();

		inputPositionCurrent.z = cameraMain.nearClipPlane
			* throwingObjectCurrent.cameraNearClipPlaneFactorOnReset;

		throwingObjectCurrent.transform.position = Vector3.Lerp(
			throwingObjectCurrent.transform.position,
			cameraMain.ScreenToWorldPoint(inputPositionCurrent),
			Time.fixedDeltaTime * lerpTimeFactorOnTouchForFlick
		);
	}

	private IEnumerator ThrowCoroutine(
		Vector2 inputPositionFirst,
		Vector2 inputPositionLast,
		ThrowingObject throwingObject)
	{
		throwingObject.transform.parent = parentOnThrow;

		if (modeAtAwake == Mode.ClickOrTap || modeAtAwake == Mode.PressKey)
		{
			yield return new WaitForFixedUpdate();

			throwingObject.SetCollidersEnabled(true);

			//DebugPrinter.Print(throwingObject.gameObject.name
			//	+ " : SetCollidersEnabled(true)");
		}

		yield return new WaitForFixedUpdate();

		if (isLayerCustomSetOnThrowAndReset)
		{
			StartCoroutine(ChangeLayerCoroutine(
				layerSettingOnThrowDelay,
				throwingObject.gameObject,
				layerIndexOnThrow));
		}

		if (characterControllerFPS)
		{
			characterControllerFPSSpeedCurrent =
				characterControllerFPS.transform.InverseTransformDirection(
					characterControllerFPS.velocity).z;

			//DebugPrinter.Print(characterControllerFPSSpeedCurrent);
		}

		//DebugPrinter.Print(characterControllerFPSSpeedCurrent);

		throwingObject.ThrowBase(
			inputPositionFirst,
			inputPositionLast,
			inputSensitivity,
			cameraMain.transform,
			Screen.height,
			forceFactorExtra + characterControllerFPSSpeedCurrent,
			torqueFactorExtra,
			torqueAngleExtra);

		//DebugPrinter.Print(throwingObject.gameObject.name
        //	+ " : IEnumerator Throw()");

		if (isFadingOn && throwingObject.materialControl)
		{
			throwingObject.materialControl.Fade(false, false);

			StartCoroutine(FadeOutCoroutine(
				throwingObject.materialControl.delayOut, throwingObject));
		}

		// Wait for physics changing
		yield return new WaitForFixedUpdate();

		throwingObject.PlayAudioWhoosh();

		OnThrow.Invoke(throwingObject);

		StartCoroutine(ResetCoroutine(resetDelay, throwingObject));

		GetNextThrow(nextThrowGettingDelay);
	}

	private IEnumerator FadeOutCoroutine(
		float delay, ThrowingObject throwingObject)
	{
		yield return new WaitForSeconds(delay);

		OnFadingOut.Invoke(throwingObject);
	}

	private IEnumerator ResetCoroutine(
		float delay, ThrowingObject throwingObject,
		bool isPositionInitial = false)
	{
		yield return new WaitForSeconds(delay);
		yield return new WaitForFixedUpdate();

		if (isLayerCustomSetOnThrowAndReset)
		{
			ChangeLayer(throwingObject.gameObject, layerIndexOnReset);

			yield return new WaitForSeconds(layerSettingOnResetFinishingDelay);
		}

		throwingObject.isThrown = false;
		throwingObject.ResetPhysicsBase();

		if (modeAtAwake == Mode.ClickOrTap || modeAtAwake == Mode.PressKey)
		{
			throwingObject.SetCollidersEnabled(false);

			//DebugPrinter.Print(throwingObject.gameObject.name
			//	+ " : SetCollidersEnabled(false)");
		}
		else
		{
			throwingObject.ActivateTriggersOnColliders(true);

			//DebugPrinter.Print(throwingObject.gameObject.name
			//	+ " : ActivateTriggersOnColliders(true)");
		}

		throwingObject.SetRendererEnabled(false);

		yield return new WaitForFixedUpdate();

		if (isPositionInitial && randomObjectPooler.positionAtInit)
		{
			throwingObject.ResetPosition(
				randomObjectPooler.positionAtInit.position);
		}
        else
        {
			throwingObject.ResetPosition(cameraMain);
		}

		throwingObject.ResetRotation(randomObjectPooler.poolParent);

		yield return new WaitForFixedUpdate();

		throwingObject.transform.parent = randomObjectPooler.poolParent;
		throwingObject.gameObject.SetActive(false);

		OnReset.Invoke(throwingObject);
	}

	private void GetNextThrow(float delay)
	{
		isNextThrowGetting = true;

		StartCoroutine(GetNextThrowCoroutine(delay));
	}

	private IEnumerator GetNextThrowCoroutine(float delay)
	{
		gameObjectCurrent = null;
		throwingObjectCurrent = null;

		yield return new WaitForSeconds(delay);

		gameObjectTemp = randomObjectPooler.GetPooledObject();

		if (gameObjectTemp)
		{
			gameObjectTemp.SetActive(true);

			throwingObjectTemp = randomObjectPooler.RegisterControlScript(
				gameObjectTemp) as ThrowingObject;

			throwingObjectTemp.ResetPosition(cameraMain);
			throwingObjectTemp.ResetRotation(randomObjectPooler.poolParent);

			//DebugPrinter.Print(
			//	throwingObjectTemp.rigidbody3D.rotation.eulerAngles);

			if (modeAtAwake == Mode.Flick)
			{
				yield return new WaitForFixedUpdate();

				throwingObjectTemp.ActivateTriggersOnColliders(false);

				//DebugPrinter.Print(throwingObjectTemp.name
				//	+ " : ActivateTriggersOnColliders(false)");
			}

			yield return new WaitForFixedUpdate();

			throwingObjectTemp.SetRendererEnabled(true);

			if (isFadingOn && throwingObjectTemp.materialControl)
			{
				throwingObjectTemp.materialControl.Fade(true, true, true);
			}

			// ---------------------
			// DEBUGGING OF POSITION
			// ---------------------

			// #if DEBUG
			// positionDebugger.DebugFloatAbsChanging(
			//	2f, throwingObjectTemp.rigidbody3D.position.x);
			// #endif

			throwingObjectCurrent = throwingObjectTemp;
			gameObjectCurrent = gameObjectTemp;

			isNextThrowGetting = false;

			OnNextThrowGetting.Invoke(throwingObjectCurrent);
		}
		else
		{
			//DebugPrinter.Print("GetNextThrowBase() => false");

			StartCoroutine(GetNextThrowCoroutine(nextCoroutineCallTryDelay));
		}
	}

	public void PlayRandomSoundDependingOnSpeed(
		int index,
		GameObject to,
		bool isStoppedBeforePlay)
	{
		ThrowingObject throwingObjectTemp =
			randomObjectPooler.RegisterControlScript(to) as ThrowingObject;

		if (throwingObjectTemp)
		{
			throwingObjectTemp.PlayAudioRandomlyDependingOnSpeed(
				index, isStoppedBeforePlay);
		}
	}

	public void PlayRandomSoundDependingOnSpeed(
		ThrowingObject.AudioData audioData,
		GameObject to,
		bool isStoppedBeforePlay)
	{
		ThrowingObject throwingObjectTemp =
			randomObjectPooler.RegisterControlScript(to) as ThrowingObject;

		if (throwingObjectTemp)
		{
			throwingObjectTemp.PlayAudioRandomlyDependingOnSpeed(
				audioData, isStoppedBeforePlay);
		}
	}

	public void SetMaterial(Material material, GameObject to)
	{
		ThrowingObject throwingObjectTemp =
			randomObjectPooler.RegisterControlScript(to) as ThrowingObject;

		if (throwingObjectTemp)
		{
			throwingObjectTemp.SetMaterial(material);
		}
	}

	public void SetMaterial(int index, GameObject to)
	{
		ThrowingObject throwingObjectTemp =
			randomObjectPooler.RegisterControlScript(to) as ThrowingObject;

		if (throwingObjectTemp)
		{
			throwingObjectTemp.SetMaterial(index);
		}
	}

	private IEnumerator ChangeLayerCoroutine(
		float delay, GameObject to, int layerIndex)
	{
		yield return new WaitForSeconds(delay);

		ChangeLayer(to, layerIndex);
	}

	private void ChangeLayer(GameObject to, int layerIndex)
	{
		to.layer = layerIndex;

		//DebugPrinter.Print(layerIndex);
	}

	private int LayerMaskValueToIndex(int value)
	{
		return Mathf.RoundToInt(Mathf.Log(value, 2));
	}

	private void InitLayerIndexes()
	{
		layerIndexOnInit = LayerMaskValueToIndex(layerOnInit.value);
		layerIndexOnThrow = LayerMaskValueToIndex(layerOnThrow.value);
		layerIndexOnReset = LayerMaskValueToIndex(layerOnReset.value);
	}

#if DEBUG

	public void TestEvent(int i)
	{
		DebugPrinter.Print(
			"Event Call: " + i + ", " + System.DateTime.Now.TimeOfDay);
	}

#endif

	private bool IsPointerOverUIObject(Vector2 touchPosition)
	{
		if (isThrowingBlockedWhenClickingUI)
		{
			PointerEventData pointerEventData =
				new(EventSystem.current)
				{
					position = touchPosition
				};

			List<RaycastResult> raycastResults = new();

			EventSystem.current.RaycastAll(pointerEventData, raycastResults);

			return raycastResults.Count > 0;
		}
		else
		{
			return false;
		}
	}

	public int GetObjectCount()
    {
		return randomObjectPooler.controlScripts.Count;
    }

	public void SetPrefabBeforeInit(GameObject gameObject)
	{
		randomObjectPooler.prefab = gameObject;
	}

	public void SetPrefabsBeforeInit(GameObject[] gameObjects)
	{
		randomObjectPooler.prefabs = gameObjects;
	}
}