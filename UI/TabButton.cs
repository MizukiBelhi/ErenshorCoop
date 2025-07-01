using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ErenshorCoop.UI
{
	public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private RectTransform rectTransform;
		private Vector2 originalLocalPosition;
		private float hoverOffset = 2f;
		private float selectionOffset = 6f;

		private bool _isSelected = false;

		void Awake()
		{
			rectTransform = GetComponent<RectTransform>();
		}

		public void SetOriginalPosition(Vector2 pos)
		{
			originalLocalPosition = pos;
			rectTransform.anchoredPosition = originalLocalPosition;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (!_isSelected)
			{
				rectTransform.anchoredPosition = originalLocalPosition + Vector2.up * hoverOffset;
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (!_isSelected)
			{
				rectTransform.anchoredPosition = originalLocalPosition;
			}
		}
		public void SetSelectionState(bool selected)
		{
			_isSelected = selected;
			if (selected)
			{
				rectTransform.anchoredPosition = originalLocalPosition + Vector2.up * selectionOffset;
			}
			else
			{
				rectTransform.anchoredPosition = originalLocalPosition;
			}
		} 
	}
}
