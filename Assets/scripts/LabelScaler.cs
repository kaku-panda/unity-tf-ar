using UnityEngine;
using TMPro;
 
public class TextMeshController : MonoBehaviour {
 
	private TextMeshProUGUI box_name;
	//サイズ取得用箱
	private Vector2 box_xy;
 
	public void GetTextBoxSize()
	{
		box_name = this.GetComponent<TextMeshProUGUI>();
        box_name.rectTransform.sizeDelta = new Vector2(box_name.preferredWidth,box_name.preferredHeight);
		Debug.Log(box_name.rectTransform.sizeDelta);
	}
}