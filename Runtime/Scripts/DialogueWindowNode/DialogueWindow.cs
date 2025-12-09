using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// Dialogue Window system running as a Singleton that could be automatically loaded if it was not preloaded by placing it's prefab into starting scene.
/// </summary>
public class DialogueWindow : SingletonBase<DialogueWindow>
{
    #region ACTIONS
    //Unity Events to expand functionality and add animations or components to window.
    public UnityEvent OnShowEvent;
    public UnityEvent OnCloseEvent;
    public UnityEvent OnNextButtonPressEvent;

    //Runtime actions that could be subscribed to
    public Action OnClose;
    public Action OnTypewriterBegin;
    public Action OnTypewriterEnd;
    public Action OnTypewriterChar;
    #endregion

    #region REFERENCES
    [SerializeField, Tooltip("Text Area with the name of the something doing the talking")] 
    private TMP_Text nameplateText;
    [SerializeField, Tooltip("Text Area with the text being spoken")] 
    private TMP_Text dialogueText;
    [SerializeField, Tooltip("Button to press to continue dialogue or skip typewriter effect")] 
    private Button nextButton;
    [SerializeField, Tooltip("GameObject with a tooltip that typewriter effect is finished and user can press to continue")]
    private GameObject nextTooltip;
    #endregion

    #region PARAMETERS
    [SerializeField, Tooltip("How fast characters appear when typewriter effect is running")] 
    private float charactersPerSecond = 5;
    #endregion

    #region VARIABLES
    //Text buffer
    private TMP_TextInfo dialogueTextInfo;
    //Reference to running typewriter effect
    private Coroutine typewriterCoroutine;
    #endregion

    protected override void Constructor()
    {
        //Initialize Events
        OnTypewriterBegin += OnTypingBegin;
        OnTypewriterChar += OnCaretMove;
        OnTypewriterEnd += OnTypingEnd;

        nextButton.onClick.AddListener(() => OnNextButtonPressEvent.Invoke());
        OnNextButtonPressEvent.AddListener(OnNextButtonPress);
    }

    #region SHOW_CLOSE_METHODS
    /// <summary>
    /// Call Dialogue Window to show on screen
    /// </summary>
    /// <param name="speaker">Name of the source of text</param>
    /// <param name="text">Text of dialogue screen</param>
    /// <param name="onShow">Performed when Dialogue Window is open</param>
    /// <param name="onClose">Performed when Dialogue Window is closed</param>
    public static void ShowDialogue(LocalizedString speaker, LocalizedString text, Action onShow = null, Action onClose = null)
    {
        ShowDialogue(speaker.GetLocalizedString(), text.GetLocalizedString(), onShow, onClose);
    }

    /// <summary>
    /// Call Dialogue Window to show on screen
    /// </summary>
    /// <param name="speaker">Name of the source of text. Can be empty.</param>
    /// <param name="text">Text of dialogue screen. Can be empty.</param>
    /// <param name="onShow">Performed when Dialogue Window is open</param>
    /// <param name="onClose">Performed when Dialogue Window is closed</param>
    public static void ShowDialogue(string speaker, string text, Action onShow = null, Action onClose = null)
    {
        Instance.OnShowEvent.Invoke();

        Instance.nameplateText.text = speaker;
        Instance.dialogueText.text = text;
        Instance.dialogueTextInfo = Instance.dialogueText.GetTextInfo(text);
        //Start typewriter effect to display dialogue text
        Instance.typewriterCoroutine = Instance.StartCoroutine(Instance.TypeText());

        onShow?.Invoke();
        Instance.OnClose = onClose;
    }

    /// <summary>
    /// Call Dialogue Window to close and hide from screen
    /// </summary>
    /// <param name="onNext"></param>
    public static void CloseDialogue(Action onNext = null)
    {
        Instance.OnCloseEvent.Invoke();

        onNext?.Invoke();
        Instance.OnClose?.Invoke();
    }
    #endregion

    #region EVENT_METHODS
    /// <summary>
    /// Called when user pressed button to close dialogue
    /// </summary>
    private void OnNextButtonPress()
    {
        //If typewriter effect is not finished, skip it.
        //If finished, Close dialogue. 
        if (typewriterCoroutine != null)
        {
            SkipTypewriter();
        }
        else
        {
            CloseDialogue(null);
        }
    }

    /// <summary>
    /// Called when typewriter effect started
    /// </summary>
    private void OnTypingBegin()
    {
        nextTooltip.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when typewriter effect adds new character to the text.
    /// Example: play sound for each char
    /// </summary>
    private void OnCaretMove()
    {

    }

    /// <summary>
    /// Called when all text shown by typewriter
    /// </summary>
    private void OnTypingEnd()
    {
        nextTooltip.gameObject.SetActive(true);
        typewriterCoroutine = null;
    }
    #endregion

    #region TYPEWRITER
    /// <summary>
    /// Typewriter Coroutine. Applies effect to TextString property.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TypeText()
    {
        OnTypewriterBegin.Invoke();

        int characterCount = dialogueTextInfo.characterCount;

        //Show one more character and wait for next cycle
        for (int i = 0; i < characterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            OnTypewriterChar.Invoke();

            yield return new WaitForSeconds(1f / charactersPerSecond);
        }

        OnTypewriterEnd.Invoke();
    }

    /// <summary>
    /// Call this to skip typewriter effect, stop coroutine and show full textString in text field.
    /// </summary>
    private void SkipTypewriter()
    {
        //Show full text
        dialogueText.maxVisibleCharacters = dialogueTextInfo.characterCount;

        //Stop typewriter coroutine if running
        if(typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            OnTypewriterEnd.Invoke();
        }
    }
    #endregion
}

#region SINGLETON
/// <summary>
/// Base class for persistent singleton MonoBehaviours
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
{
    public static T Instance
    {
        get 
        { 
            //Automatically initialize Singleton if no instance is present. Dummy protection.
            //Instantiates addressable prefab with address same as this class name.
            //Addressable asset address is shown below prefab name in editor when addressable is ticked.
            //Example: Not initialized or started Play Mode not from initialization scene
            if(instance == null)
            {
                Debug.LogWarning($"{typeof(T).Name} INSTANCE NOT FOUND IN STARTING SCENE. YOU CAN USE ADDRESSABLES ADDRESS {typeof(T).Name} TO AUTOMATICALLY LOAD INSTANCE.");

                var handle = Addressables.InstantiateAsync(typeof(T).Name);
                handle.WaitForCompletion();
                instance = handle.Result.GetComponent<T>();
                return instance;
            }
            return instance; 
        }
    }

    private static T instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = (T)this;

            // Init the singleton
            instance.Constructor();

            // The singleton object shouldn't be destroyed when we switch between scenes
            DontDestroyOnLoad(this.gameObject);
        }
        // Because we implement MonoBehaviour we might accidentially have several of them to the scene or/and DontDestroyOnLoad,
        // which will cause disruption, so we have to make sure we have just one!
        else
        {
            Destroy(gameObject);
        }
    }

    //Because this script implements MonoBehaviour, we cant use a constructor, so we have to cover functionality of one
    protected virtual void Constructor()
    {

    }
}
#endregion
