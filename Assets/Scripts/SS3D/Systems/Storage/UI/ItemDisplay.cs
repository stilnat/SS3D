﻿using System;
using SS3D.Systems.Storage.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Systems.Storage.UI
{
    /// <summary>
    /// Shows an item and allows actions such as dragging
    /// </summary>
    public class ItemDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        public Image ItemImage;
        [NonSerialized] public bool DropAccepted;
        [NonSerialized] public Vector3 OldPosition;

        protected InventoryDisplayElement InventoryDisplayElement;
        
        [SerializeField] private Item _item;
        private Transform _oldParent;
        private Vector3 _startMousePosition;
        private Vector3 _startPosition;
        private Image _slotImage;
        private Outline _outlineInner;
        private Outline _outlineOuter;


        public Item Item
        {
            get => _item;
            set
            {
                _item = value;
                UpdateDisplay();
            }
        }

        public void Start()
        {
            _slotImage = GetComponent<Image>();
            if(!_outlineInner)
            {
                _outlineInner = ItemImage.gameObject.AddComponent<Outline>();
                _outlineInner.effectColor = new Color(0, 0, 0, 0.4f);
                _outlineInner.effectDistance = new Vector2(0.6f, 0.6f);
            }
            if(!_outlineOuter)
            {
                _outlineInner = ItemImage.gameObject.AddComponent<Outline>();
                _outlineInner.effectColor = new Color(0, 0, 0, 0.2f);
                _outlineInner.effectDistance = new Vector2(0.8f, 0.8f);
            }
            if (_item != null)
            {
                UpdateDisplay();
            } 
        }
        
        public virtual void OnDropAccepted(){}
        
        public void OnPointerDown(PointerEventData eventData)
        {
            _startPosition = transform.position;
            _startMousePosition = Input.mousePosition;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // WOW Unity, this is some amazing UI stuff
            IPointerClickHandler pointerDownHandler = transform.parent.GetComponentInParent<IPointerClickHandler>();
            pointerDownHandler?.OnPointerClick(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _oldParent = transform.parent;
            if (InventoryDisplayElement == null)
            {
                InventoryDisplayElement = _oldParent.GetComponentInParent<InventoryDisplayElement>();
            }
            
            OldPosition = GetComponent<RectTransform>().localPosition;
            Vector3 tempPosition = transform.position;
            transform.SetParent(transform.root, false);
            transform.position = tempPosition;
            
            _slotImage.raycastTarget = false;
            DropAccepted = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 diff = Input.mousePosition - _startMousePosition;
            transform.position = _startPosition + diff;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            _slotImage.raycastTarget = true;
            
            if (DropAccepted)
            {
                OnDropAccepted();
                return;
            }
            
            transform.SetParent(_oldParent, false);
            GetComponent<RectTransform>().localPosition = OldPosition;

            GameObject o = eventData.pointerCurrentRaycast.gameObject;
            if (o == null)
            {
                GetComponentInParent<InventoryDisplayElement>().DropItemOutside(Item);   
            }
        }
        
        private void UpdateDisplay()
        {
            ItemImage.sprite = Item != null ? Item.InventorySprite : null;
            
            Color imageColor = ItemImage.color;
            imageColor.a = ItemImage.sprite != null ? 255 : 0;
            ItemImage.color = imageColor;
        }

    }
}