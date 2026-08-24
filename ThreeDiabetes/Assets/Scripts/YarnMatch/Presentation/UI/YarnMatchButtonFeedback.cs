using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public sealed class YarnMatchButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private RectTransform _rect;
    private Vector3 _baseScale;
    private float _targetScale = 1f;
    private float _releaseTime;
    private bool _animationDriven;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _baseScale = _rect.localScale;
    }

    private void OnEnable()
    {
        if (_rect == null)
        {
            return;
        }

        _baseScale = _rect.localScale;
        _targetScale = 1f;
        _releaseTime = 0f;
        _animationDriven = false;
    }

    internal void SetAnimationDriven(bool animationDriven)
    {
        _animationDriven = animationDriven;
        if (animationDriven)
        {
            _targetScale = 1f;
            _releaseTime = 0f;
        }
        else if (_rect != null)
        {
            _baseScale = _rect.localScale;
        }
    }

    private void Update()
    {
        if (_rect == null || _animationDriven)
        {
            return;
        }

        float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 22f);
        if (_releaseTime > 0f)
        {
            _releaseTime -= Time.unscaledDeltaTime;
        }
        else if (_targetScale > 1f)
        {
            _targetScale = 1f;
        }
        _rect.localScale = Vector3.Lerp(_rect.localScale, _baseScale * _targetScale, blend);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _targetScale = 0.92f;
        _releaseTime = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _targetScale = 1.04f;
        _releaseTime = 0.08f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = 1f;
    }
}
