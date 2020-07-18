/*
 * ButtonUtility.cs
 * Scott Duman
 * Chase Kurkowski
 * This script improves button functionality.
 */
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class ButtonUtility : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public UnityEvent onMouseEnter, onMouseExit;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        onMouseEnter = new UnityEvent();
        onMouseExit = new UnityEvent();
    }
    
    public Button.ButtonClickedEvent onClick
    {
        get {return button.onClick;}
    }

    public bool interactable
    {
        get {return button.interactable;}
        set {button.interactable = value;}
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if (onMouseEnter != null)
        {
            onMouseEnter.Invoke();
        }
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if (onMouseExit != null)
        {
            onMouseExit.Invoke();
        }
    }
}
