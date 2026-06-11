using Caretaker.Gameplay;
using Caretaker.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 조사 결과의 제목, 본문, 선택적 이미지를 로컬 모달 팝업으로 표시합니다.
    /// </summary>
    /// <remarks>
    /// HUD Shell의 modal 영역에 배치되며, 팝업이 열려 있는 동안 로컬 플레이어 입력을 잠급니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ExaminePopupPresenter : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private GameObject _imageRoot;
        [SerializeField] private Image _image;
        [SerializeField] private Button _closeButton;

        private GameObject _ownedEventSystem;
        private PlayerController _playerController;
        private int _openedFrame = -1;

        /// <summary>
        /// 조사 팝업이 현재 열려 있는지 반환합니다.
        /// </summary>
        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            EnsureEventSystem();

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_panelRoot != null)
            {
                InteractableObject.OnExamineRequested += HandleExamineRequested;
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == _openedFrame)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        private void OnDisable()
        {
            InteractableObject.OnExamineRequested -= HandleExamineRequested;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }

            Close();
            BindPlayer(null);
        }

        private void OnDestroy()
        {
            if (_ownedEventSystem == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_ownedEventSystem);
                return;
            }

            DestroyImmediate(_ownedEventSystem);
        }

        /// <summary>
        /// 전달된 조사 데이터를 팝업에 렌더링하고 게임플레이 입력을 잠급니다.
        /// </summary>
        public void Open(string title, string body, Sprite image)
        {
            if (_panelRoot == null)
            {
                Debug.LogWarning("ExaminePopupPresenter requires a panel root.", this);
                return;
            }

            EnsureEventSystem();

            if (_titleText != null)
            {
                _titleText.text = title ?? string.Empty;
            }

            if (_bodyText != null)
            {
                _bodyText.text = body ?? string.Empty;
                RectTransform bodyRect = _bodyText.rectTransform;
                bool hasImage = image != null;
                bodyRect.anchorMin = new Vector2(hasImage ? 0.45f : 0f, 0f);
                bodyRect.offsetMin = new Vector2(hasImage ? 12f : 32f, 76f);
            }

            bool shouldShowImage = image != null;
            if (_imageRoot != null)
            {
                _imageRoot.SetActive(shouldShowImage);
            }

            if (_image != null)
            {
                _image.sprite = image;
                _image.preserveAspect = true;
            }

            _panelRoot.SetActive(true);
            _openedFrame = Time.frameCount;
            SetPlayerInputBlocked(true);
        }

        /// <summary>
        /// 조사 팝업을 닫고 게임플레이 입력을 복원합니다.
        /// </summary>
        public void Close()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_image != null)
            {
                _image.sprite = null;
            }

            _openedFrame = -1;
            SetPlayerInputBlocked(false);
        }

        private void HandleExamineRequested(InteractableObject target, PlayerController actor)
        {
            if (_panelRoot == null)
            {
                InteractableObject.OnExamineRequested -= HandleExamineRequested;
                return;
            }

            if (target == null)
            {
                return;
            }

            BindPlayer(actor);
            Open(target.ObjectId, target.ExamineText, target.ExamineImage);
        }

        private void BindPlayer(PlayerController nextPlayerController)
        {
            if (_playerController == nextPlayerController)
            {
                return;
            }

            if (_playerController != null)
            {
                _playerController.SetInputBlocked(false);
            }

            _playerController = nextPlayerController;

            if (_playerController != null)
            {
                _playerController.SetInputBlocked(IsOpen);
            }
        }

        private void SetPlayerInputBlocked(bool isBlocked)
        {
            if (_playerController != null)
            {
                _playerController.SetInputBlocked(isBlocked);
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            _ownedEventSystem = new GameObject(
                "ExaminePopupEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
        }
    }
}
