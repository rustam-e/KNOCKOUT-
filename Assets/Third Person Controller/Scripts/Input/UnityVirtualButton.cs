﻿using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Opsive.ThirdPersonController.Input
{
    /// <summary>
    /// Acts as a virtual button for UnityMobileInput.
    /// </summary>
    public class UnityVirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        /// <summary>
        /// Specifies if the VirtualButton can be pressed or if it should have a swipe functionality.
        /// </summary>
        public enum TouchType { Press, RelativeHorizontal, RelativeVertical, Horizontal, Vertical, HorizontalJoystick, VerticalJoystick }

        /// <summary>
        /// A mapping of the VirtualButton name to the TouchType.
        /// </summary>
        [System.Serializable]
        private class VirtualButtonType
        {
            [Tooltip("The name of the VirtualButton")]
            [SerializeField] private string m_Name;
            [Tooltip("The type of VirtualButton")]
            [SerializeField] private TouchType m_TouchType;

            // Exposed properties
            public TouchType TouchType { get { return m_TouchType; } }
            public string Name { get { return m_Name; } }
        }

        // A single VirtualButton can have multiple mappings depending on what it should do")]
        [SerializeField] private VirtualButtonType[] m_VirtualButtonType;
        [Tooltip("Used if the TouchType is RelativeHorizontal or RelativeVertical, this value specifies how quickly the axis can move between values")]
        [SerializeField] private float m_InterpolateTime;
        [Tooltip("Used if the TouchType is RelativeHorizontal or RelativeVertical, this value specifies how far a swipe has to be to be considered a full swipe " +
                 "thus returning the maximum axis value")]
        [SerializeField] private float m_FullSwipeDistance;
        [Tooltip("Used if the TouchType is Horizontal or Vertical, this value specifies how sensitive the swipe is")]
        [SerializeField] private float m_SwipeSensitivity;
        [Tooltip("Used if the TouchType is Joystick, this value specifies how far the joystick can move from its center position")]
        [SerializeField] private int m_MovementRange = 100;

        // Internal variables
        private Dictionary<string, TouchType> m_TouchTypeMap;
        private Vector2 m_LastPosition;
        private int m_FingerID = -1;
        private bool m_Pressed;
        private int m_LastPressedFrame = -1;
        private int m_ReleasedFrame;
		private Vector2 m_DeltaPosition;

        // Component references
        private RectTransform m_RectTransform;
        private UnityInput m_UnityInput;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        private void Awake()
        {
			m_RectTransform = GetComponent<RectTransform>();
            m_UnityInput = PlayerInput.Instance as UnityInput;

            m_LastPosition = m_RectTransform.anchoredPosition;
        }

        /// <summary>
        /// Rgister the VirtualButton and setup the mapping.
        /// </summary>
        private void OnEnable()
        {
            m_TouchTypeMap = new Dictionary<string, TouchType>();
            for (int i = 0; i < m_VirtualButtonType.Length; ++i) {
                m_UnityInput.RegisterVirtualButton(m_VirtualButtonType[i].Name, this);
                m_TouchTypeMap.Add(m_VirtualButtonType[i].Name, m_VirtualButtonType[i].TouchType);
            }
		}
		
		/// <summary>
        /// Unregister the VirtualButton and clear the mapping.
        /// </summary>
        private void OnDisable()
        {
            for (int i = 0; i < m_VirtualButtonType.Length; ++i) {
                m_UnityInput.UnregisterVirtualButton(m_VirtualButtonType[i].Name);
            }

            m_TouchTypeMap = null;
        }

        /// <summary>
        /// Callback when a finger has pressed on the button.
        /// </summary>
        /// <param name="data">The finger data.</param>
        public void OnPointerDown(PointerEventData data)
        {
            if (m_Pressed) {
                return;
            }
            m_Pressed = true;
            m_LastPressedFrame = Time.frameCount;
			m_DeltaPosition = Vector2.zero;
            // Unity Remote doesn't correctly fill in the pointer ID. Use the touchCount instead.
#if UNITY_EDITOR
            m_FingerID = UnityEngine.Input.touchCount - 1;
            // If the touchCount is still -1 then the mouse is being used.
            if (m_FingerID == -1) {
                m_FingerID = 0;
            }
#else
            m_FingerID = data.pointerId;
#endif
        }

        /// <summary>
        /// Callback when a finger has released the button.
        /// </summary>
        /// <param name="data">The finger data.</param>
        public void OnPointerUp(PointerEventData data)
        {
			m_Pressed = false;
			m_ReleasedFrame = Time.frameCount;
            m_FingerID = -1;
            var isJoystick = false;
            for (int i = 0; i < m_VirtualButtonType.Length; ++i) {
                if (m_VirtualButtonType[i].TouchType == TouchType.HorizontalJoystick || m_VirtualButtonType[i].TouchType == TouchType.VerticalJoystick) {
                    isJoystick = true;
                    break;
                }
            }
            // The joystick may have moved so snap back to the original position.
            if (isJoystick) {
                m_RectTransform.anchoredPosition = m_LastPosition;
            }
        }

        /// <summary>
        /// Callback when a finger has dragged the button.
        /// </summary>
        /// <param name="data">The finger data.</param>
        public void OnDrag(PointerEventData data)
        {
			// Only the joystick can change positions.
            for (int i = 0; i < m_VirtualButtonType.Length; ++i) {
                if (m_VirtualButtonType[i].TouchType != TouchType.HorizontalJoystick && m_VirtualButtonType[i].TouchType != TouchType.VerticalJoystick) {
                    return;
                }
            }

			m_DeltaPosition += data.delta;
			m_DeltaPosition.x = Mathf.Clamp(m_DeltaPosition.x, -m_MovementRange, m_MovementRange);
			m_DeltaPosition.y = Mathf.Clamp(m_DeltaPosition.y, -m_MovementRange, m_MovementRange);
			var position = m_RectTransform.anchoredPosition;
			position.x = m_LastPosition.x + m_DeltaPosition.x;
			position.y = m_LastPosition.y + m_DeltaPosition.y;
			m_RectTransform.anchoredPosition = position;
        }

        /// <summary>
        /// Returns true if a finger is touching the button.
        /// </summary>
        /// <returns>True if a finger is touching the button.</returns>
        public bool GetButton()
        {
            return m_Pressed;
        }

        /// <summary>
        /// Returns true if a finger just touched the button.
        /// </summary>
        /// <returns>True if a finger just touched the button.</returns>
        public bool GetButtonDown()
        {
            return m_LastPressedFrame - Time.frameCount == -1;
        }

        /// <summary>
        /// Returns true if a finger is not touching the button.
        /// </summary>
        /// <returns>True if a finger is not touching the button.</returns>
        public bool GetButtonUp()
        {
            return (m_ReleasedFrame == Time.frameCount - 1);
        }

        /// <summary>
        /// Returns the axis value. The axis is determined by the TouchType and is relative to the start position or the delta position.
        /// </summary>
        /// <param name="name">The name of the VirtualButton.</param>
        /// <returns>The axis value.</returns>
        public float GetAxis(string name)
        {
            TouchType touchType;
            if (m_TouchTypeMap.TryGetValue(name, out touchType)) {
#if UNITY_EDITOR
                if (m_FingerID > -1) {
#else
                if (UnityEngine.Input.touchCount >= m_FingerID + 1 && m_FingerID > -1) {
#endif
                    Vector3 position;
#if UNITY_EDITOR
                    if (UnityEngine.Input.touchCount == 0) {
                        position = UnityEngine.Input.mousePosition - (Vector3)m_UnityInput.GetLastTouchPosition();
                    } else {
#endif
                        var touch = UnityEngine.Input.touches[m_FingerID];
                        if (touchType == TouchType.RelativeHorizontal || touchType == TouchType.RelativeVertical) {
                            // The finger id is the current finger id so smootly interpolate the axis value based on a relative position.
                            position = (touch.position - m_UnityInput.GetTouchStartPosition(m_FingerID)) / m_FullSwipeDistance;
                        } else {
                            position = touch.deltaPosition * m_SwipeSensitivity;
                        }
#if UNITY_EDITOR
                    }
#endif
                    float delta;
                    switch (touchType) {
                        case TouchType.RelativeHorizontal:
                        case TouchType.Horizontal:
                            m_LastPosition.x = Mathf.Clamp(Mathf.Lerp(m_LastPosition.x, position.x, m_InterpolateTime * Time.deltaTime), -1, 1);
                            return m_LastPosition.x;
                        case TouchType.RelativeVertical:
                        case TouchType.Vertical:
                            m_LastPosition.y = Mathf.Clamp(Mathf.Lerp(m_LastPosition.y, position.y, m_InterpolateTime * Time.deltaTime), -1, 1);
                            return m_LastPosition.y;
                        case TouchType.HorizontalJoystick:
                            delta = m_RectTransform.anchoredPosition.x - m_LastPosition.x;
                            delta /= m_MovementRange;
                            return delta;
                        case TouchType.VerticalJoystick:
                            delta = m_RectTransform.anchoredPosition.y - m_LastPosition.y;
                            delta /= m_MovementRange;
                            return delta;
                    }
                }
                // The finger id wasn't found so smoothly move the axis back to zero.
                m_FingerID = -1;
                switch (touchType) {
                    case TouchType.RelativeHorizontal:
                        if (m_LastPosition.x != 0) {
                            m_LastPosition.x = Mathf.Max(m_LastPosition.x - (Time.deltaTime * m_InterpolateTime), 0);
                            return m_LastPosition.x;
                        }
                        break;
                    case TouchType.RelativeVertical:
                        if (m_LastPosition.y != 0) {
                            m_LastPosition.y = Mathf.Max(m_LastPosition.y - (Time.deltaTime * m_InterpolateTime), 0);
                            return m_LastPosition.x;
                        }
                        break;
                }
                return 0;
            }
            return 0;
        }
    }
}
