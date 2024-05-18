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
using UnityEngine.Events;

[HelpURL("https://makaka.org/unity-assets")]
public class OneTimeEventControl : MonoBehaviour 
{
	[TextArea(2,3)]
	[SerializeField]
	private string description;

	[Space]
	[SerializeField]
	private bool isDebugLogging = false;

    [SerializeField]
    private bool isExecutedAgain = false;

    [Space]
	[SerializeField]
	private KeyCode oneTimeFunctionKey = KeyCode.Return;

	[Space]
	[SerializeField]
	private UnityEvent OnPressOneTimeFunctionKey;

	private bool isOneTimeFunctionCalled = false;

    private void Update() 
	{
		if (Input.GetKeyDown(oneTimeFunctionKey))
		{
			if (isExecutedAgain)
			{
                ExecuteAgain();
            }
			else
			{
                Execute();
            }
        }
	}

	public void Execute()
    {
		if (!isOneTimeFunctionCalled)
		{
			ExecuteAgain();
        }
	}

	public void ExecuteAgain()
	{
        isOneTimeFunctionCalled = true;

        if (isDebugLogging)
        {
            DebugPrinter.Print(description);
        }

        OnPressOneTimeFunctionKey.Invoke();
    }
}
