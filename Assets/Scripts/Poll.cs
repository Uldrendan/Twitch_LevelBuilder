﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TwitchChatter;
using System;

public class Poll : MonoBehaviour {




	public Sprite iceIcon;
	public Sprite fireIcon;

    public static Poll Instance;

    public string _channel;

    public Text _iceCountDisplay;
    public Text _fireCountDisplay;
    public Text _weaponDisplay;


	public const string iceCommand = "#ice";
	public const string fireCommand = "#fire";

	public const string hammerCommand = "#hammer";
	public const string daggerCommand = "#dagger";
	public const string whipCommand = "#whip";

	public string hammerText = "Hammer";
	public string daggerText = "Dagger";
	public string whipText = "Whip";

	private List<string> _voterListElement;
	private List<string> _voterListWeapon;

    void Awake()
    {
		if (Instance != null) {
			Destroy (Instance);
		} else {
			Instance = this;
		}

        _voterListElement = new List<string>();
		_voterListWeapon = new List<string>();
    }

    void Start () {
		_channel = GameMaster.Instance.username;

        if (TwitchChatClient.singleton != null)
        {
            TwitchChatClient.singleton.AddChatListener(OnChatMessage);
        }

        if (!string.IsNullOrEmpty(_channel))
        {
            TwitchChatClient.singleton.JoinChannel(_channel);
        }
        else
        {
            Debug.LogWarning("No channel name entered for poll! Enter a channel name and restart the scene.", this);
        }
			
    }

	void Update() {
		float CanvasWidth = GetComponent<CanvasScaler>().referenceResolution.x;

		// make shout bubbles float
		foreach (Transform child in transform) {
			if (child.CompareTag ("Shout Bubble")) {
				Vector2 bubbleAnchoredPosition = child.GetComponent<RectTransform> ().anchoredPosition;

				// delete bubble if it is out of the screen
				if (bubbleAnchoredPosition.x < -CanvasWidth * 0.55f) {
					Destroy (child.gameObject);
				} else {
					// move bubble
					child.GetComponent<RectTransform> ().anchoredPosition = new Vector2 (bubbleAnchoredPosition.x - Time.deltaTime * CanvasWidth / 3, bubbleAnchoredPosition.y);
				}
			}
		}
	}
		
    private void OnDestroy()
    {
        if (TwitchChatClient.singleton != null)
        {
            TwitchChatClient.singleton.RemoveChatListener(OnChatMessage);
        }
    }

    private void OnChatMessage(ref TwitchChatMessage msg)
    {
        if (!_voterListElement.Contains(msg.userName))
        {
            bool isValidVote = false;

            if (msg.chatMessagePlainText.Equals(iceCommand, StringComparison.InvariantCultureIgnoreCase)) {
                isValidVote = true;
                ++GameMaster.Instance.iceCount;
                _iceCountDisplay.text = "" + GameMaster.Instance.iceCount;
				createShoutBubble (iceCommand);
            } else if (msg.chatMessagePlainText.Equals(fireCommand, StringComparison.InvariantCultureIgnoreCase)) {
                isValidVote = true;
                ++GameMaster.Instance.fireCount;
                _fireCountDisplay.text = "" + GameMaster.Instance.fireCount;
				createShoutBubble (fireCommand);
            }

            if (isValidVote) {
                _voterListElement.Add(msg.userName);
            }
        }

		if (!_voterListWeapon.Contains(msg.userName))
		{
			bool isValidVote = false;

			if (msg.chatMessagePlainText.Equals(hammerCommand, StringComparison.InvariantCultureIgnoreCase)) {
				isValidVote = true;
				++GameMaster.Instance.hammerCount;
				if (GameMaster.Instance.hammerCount > GameMaster.Instance.daggerCount &&
					GameMaster.Instance.hammerCount > GameMaster.Instance.whipCount)
					_weaponDisplay.text = hammerText;
			} else if (msg.chatMessagePlainText.Equals(daggerCommand, StringComparison.InvariantCultureIgnoreCase)) {
				isValidVote = true;
				++GameMaster.Instance.daggerCount;
				if (GameMaster.Instance.daggerCount > GameMaster.Instance.hammerCount &&
					GameMaster.Instance.daggerCount > GameMaster.Instance.whipCount)
					_weaponDisplay.text = daggerText;
			} else if (msg.chatMessagePlainText.Equals(whipCommand, StringComparison.InvariantCultureIgnoreCase)) {
				isValidVote = true;
				++GameMaster.Instance.whipCount;
				if (GameMaster.Instance.whipCount > GameMaster.Instance.hammerCount &&
					GameMaster.Instance.whipCount > GameMaster.Instance.daggerCount)
					_weaponDisplay.text = whipText;
			}

			if (isValidVote)
			{
				_voterListWeapon.Add(msg.userName);
			}
		}
    }

	private void createShoutBubble (string command) {

		float CanvasWidth = GetComponent<CanvasScaler>().referenceResolution.x;
		float CanvasHeight = GetComponent<CanvasScaler>().referenceResolution.y;
		float bubbleSize = CanvasHeight / 7.0f; // size of shout bubble relative to screen size;

		// center coordinates (x, y) of the icon bubble
		float x = UnityEngine.Random.Range (CanvasWidth * 0.55f, CanvasWidth * 0.60f);
		float y = UnityEngine.Random.Range (-CanvasHeight * 0.22f, -CanvasHeight * 0.33f);
	
		// instantiate shout bubble
		instantiateBubble ((command == iceCommand)? iceIcon : fireIcon, x, y, bubbleSize, bubbleSize);
	}

	// create UI element based on the lowerleft coordinates (x, y), and its width and height
	private GameObject instantiateBubble(Sprite sprite, float x, float y, float width, float height)
	{
		GameObject bubble = new GameObject ();
		bubble.AddComponent<RectTransform> ();
		bubble.AddComponent<Image> ();
		bubble.GetComponent<Image> ().sprite = sprite;
		bubble.tag = "Shout Bubble";
		GameObject newUI = Instantiate (bubble);
		newUI.transform.SetParent (transform);
		newUI.GetComponent<RectTransform> ().anchoredPosition = new Vector2(x, y);
		newUI.GetComponent<RectTransform> ().sizeDelta = new Vector2 (width, height);
		newUI.GetComponent<RectTransform> ().anchorMax = new Vector2 (0.5f, 0.5f);
		newUI.GetComponent<RectTransform> ().anchorMin = new Vector2 (0.5f, 0.5f);
		newUI.GetComponent<RectTransform> ().pivot = new Vector2 (0.5f, 0.5f);

		return newUI;
	}

	public void ResetVoteWeapon()
	{
		_weaponDisplay.text = "Next Weapon";

		GameMaster.Instance.daggerCount = 0;
		GameMaster.Instance.hammerCount = 0;
		GameMaster.Instance.whipCount = 0;

		_voterListWeapon.Clear();
	}

	public void ResetVoteElement()
	{
		_iceCountDisplay.text = "0";
		_fireCountDisplay.text = "0";

		GameMaster.Instance.iceCount = 0;
		GameMaster.Instance.fireCount = 0;

		_voterListElement.Clear();
	}

}