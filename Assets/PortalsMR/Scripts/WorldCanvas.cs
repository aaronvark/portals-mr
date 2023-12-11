using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class WorldCanvas : MonoBehaviour
{
    public delegate void SetText(string text);
    
    public static SetText changeCanvasText { get; private set; }

    private TextMeshPro _text;

    // Start is called before the first frame update
    void Awake()
    {
        _text = GetComponent<TextMeshPro>();
		changeCanvasText = ChangeText;
	}

    // Update is called once per frame
    void ChangeText(string newText)
    {
        _text.text = newText;
	}
}
