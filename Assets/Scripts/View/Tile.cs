using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets;
using DG.Tweening;
using UnityEngine;
using Utils;

namespace View
{
    public class Tile : MonoBehaviour
    {
        [SerializeField] private GameObject selectedSprite;
        
        public static event Action<Tile, Tile> OnReadyToSwap;

        public static bool InputLocked;
        private static Tile _selected;
        
        public bool isMoving;

        private Transform _transform;
        private SpriteRenderer _renderer;
        
        public string Name { get; private set; }
        public TileType Type { get; private set; }
        private bool IsSwappable { get; set; }
        
        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _transform = transform;
            selectedSprite.SetActive(false);
        }

        public void InitTile(TileItemData data)
        {
            if(data == null) Debug.LogError($" Tile data is null! Cannot create a tile!");
            
            Name = data.Name;
            Type = data.Type;
            _renderer.sprite = data.ItemIcon;
            IsSwappable = data.IsSwappable;
        }

        public void Reset()
        {
            Name = string.Empty;
            Type = TileType.Item;
            _renderer.sprite = null;
            IsSwappable = false;
        }
        
        public async Task Move(List<Direction> path)
        {
            isMoving = true;
            foreach (var step in path)
            {
                var nextPos = GetNextPosition(step);
                await MoveAnimation(nextPos);
            }
            EndAnimation();
        }

        public async Task Move(Vector3 newPos)
        {
            await MoveAnimation(newPos);
            EndAnimation();
        }
        
        public async Task PlayMatchedAnimation()
        {
            isMoving = true;
            var fadeSequence = DOTween.Sequence();
            fadeSequence.Append(_transform.DOScale(new Vector3(0.9f, 1.15f, 1), 0.1f));
            fadeSequence.Append(_transform.DOScale(new Vector3(1.15f, 0.9f, 1), 0.1f));
            fadeSequence.Append(_transform.DOScale(new Vector3(0.8f, 0.8f, 1), 0.1f));
            fadeSequence.Append(_transform.DOScale(new Vector3(1.4f, 1.4f, 1), 0.2f));
            fadeSequence.Append(_transform.DOScale(new Vector3(0.5f, 0.5f, 1), 0.1f));

            await Awaiters.WaitUntil(() => !fadeSequence.active);
            isMoving = false;
        } 
        
        private async Task MoveAnimation(Vector3 newPos)
        {
            isMoving = true;
            var moveTween = _transform.DOMove(newPos, 0.3f);
            var decorateSequence = DOTween.Sequence();
            decorateSequence.Append(_transform.DOScale(new Vector3(0.85f, 1.15f, 1), 0.1f));
            decorateSequence.Append(_transform.DOScale(new Vector3(0.85f, 1.1f, 1), 0.05f));
            
            await Awaiters.WaitUntil(() => !moveTween.active);
        }

        private void EndAnimation()
        {
            isMoving = false;
            var position = _transform.position;
            position = new Vector3(position.x, position.y, 0);
            _transform.position = position;
            var sequence = DOTween.Sequence();
            sequence.Append(_transform.DOScale(new Vector3(1.05f, 0.85f, 1), 0.15f));
            sequence.Append(_transform.DOScale(new Vector3(1f, 1f, 1), 0.05f));
        }
        
        private Vector3 GetNextPosition(Direction step)
        {
            var pos = _transform.position;

            return step switch
            {
                Direction.Up => pos + Vector3.up,
                Direction.UpRight => pos + Vector3.up + Vector3.right,
                Direction.UpLeft => pos + Vector3.up + Vector3.left,
                _ => Vector3.zero
            };
        }
        
        private void OnMouseUp()
        {
            if (!IsSwappable || InputLocked) { return; }

            if (_selected == null)
            {
                Select();
            }
            else
            {
                if (_selected == this)
                {
                    Unselect();
                }
                else
                {
                    if (IsNeighbour(_selected))
                    {
                        OnReadyToSwap?.Invoke(this, _selected);
                        _selected.Unselect();
                    }
                    else
                    {
                        _selected.Unselect();
                        Select();
                    }
                }
            }
        }

        private bool IsNeighbour(Tile otherTile)
        {
            var position = _transform.position;
            var targetPosition = otherTile.transform.position;
            return Math.Abs(position.x - targetPosition.x) < 2 && Mathf.Approximately(position.y,targetPosition.y) ||
                   Math.Abs(position.y - targetPosition.y) < 2 && Mathf.Approximately(position.x,targetPosition.x);
        }

        private void Select()
        {
            _selected = this;
            selectedSprite.SetActive(true);
        }

        private void Unselect()
        {
            _selected = null;
            selectedSprite.SetActive(false);
        }
    }
}