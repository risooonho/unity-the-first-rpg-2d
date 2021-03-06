﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Character : MonoBehaviour
{
    public string Name;

    public float MoveSpeed = 1f;

    public GameObject DialogueBark;

    public GameObject QuestBark;

    public BoolObject InDialogue;

    public AnimationClip IdleClip;

    public AnimationClip WalkingClip;

	public AnimationClip JumpingClip;

    public float JumpTime = 0.5f;

    public float JumpSpeed = 5.0f;

    protected Animator anim;
    protected enum MoveDirection { Up, Down, Left, Right };
    protected bool canMove;

	protected bool isWalking, isWalkingLast, isJumping, isJumpingLast;
    private bool isLeft, isLeftLast;
    private Vector3 localScale;
    private RPGEventTrigger EventTrigger;
    private static int count;
    private bool inRangeOfPlayer;
    private Player player;
    private int id;
    private bool usePhysics;
	private int layerMask;
	private Coroutine jumpingCoroutine;

    private void Awake()
    {
        id = count;
        count++;
    }

    public int ID
    {
        get
        {
            return id;
        }
    }

    protected virtual void Start()
    {
        if (QuestBark != null)
        {
            QuestBark.SetActive(false);
        }

        if (DialogueBark != null)
        {
            DialogueBark.SetActive(false);
        }

        EventTrigger = GetComponent<RPGEventTrigger>();

        if (EventTrigger != null && EventTrigger.RPGEvents.Count <= 0)
        {
            EventTrigger = null;
        }

        if (EventTrigger != null)
        {
            EventTrigger.SetName(Name);

            UpdateRPGEvent();
        }

        anim = GetComponent<Animator>();

        if (anim != null)
        {
            AnimatorOverrideController myNewOverrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
            myNewOverrideController.runtimeAnimatorController = anim.runtimeAnimatorController;
            if (IdleClip != null)
            {
                myNewOverrideController["KnightIdle"] = IdleClip;
            }
            if (WalkingClip != null)
            {
                myNewOverrideController["KnightWalking"] = WalkingClip;
            }
			if (JumpingClip != null) {
				myNewOverrideController ["KnightJumping"] = JumpingClip;
			}
            myNewOverrideController.runtimeAnimatorController = anim.runtimeAnimatorController;
            anim.runtimeAnimatorController = myNewOverrideController;
        }

        localScale = transform.localScale;

        SetCanMove(true);

        InDialogue.OnUpdated += OnInDialogueUpdated;

		layerMask = 1 << (LayerMask.NameToLayer("Map")) | 1 << (LayerMask.NameToLayer("Obstacle"));
    }

    protected virtual void Update()
    {
        isWalking = false;
    }

    protected virtual void LateUpdate()
    {
        if (isWalking != isWalkingLast)
        {
            anim.SetBool("IsWalking", isWalking);
        }
		if (isJumping != isJumpingLast) {
            if (isJumping)
			{
				anim.SetTrigger("IsJumping");
            }
		}
        isWalkingLast = isWalking;
		isJumpingLast = isJumping;

        if (isLeft != isLeftLast)
        {
            transform.localScale = new Vector3(-1 * localScale.x, localScale.y, localScale.z);
            localScale = transform.localScale;
        }
        isLeftLast = isLeft;
    }

    protected virtual void Move(MoveDirection direction)
    {
        if (canMove == false) return;

        Vector3 nextPosition = transform.position;

        switch (direction)
        {
            case MoveDirection.Up:
                nextPosition += Vector3.up * MoveSpeed * Time.deltaTime;
                break;
            case MoveDirection.Down:
                nextPosition += Vector3.down * MoveSpeed * Time.deltaTime;
                break;
            case MoveDirection.Left:
                nextPosition += Vector3.left * MoveSpeed * Time.deltaTime;
                isLeft = true;
                break;
            case MoveDirection.Right:
                nextPosition += Vector3.right * MoveSpeed * Time.deltaTime;
                isLeft = false;
                break;
        }

        if (Vector3.Distance(nextPosition, transform.position) > float.Epsilon)
        {
			if (Physics2D.Raycast(transform.position, nextPosition - transform.position, MoveSpeed * Time.deltaTime, layerMask))
			{
				// do nothing
				isWalking = false;
			}
			else
			{
				isWalking = true;
				transform.position = nextPosition;
			}
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inRangeOfPlayer = true;
            player = other.GetComponent<Player>();

            if (player != null)
            {
                player.Pickup -= OnPickupHandler;
                player.Pickup += OnPickupHandler;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inRangeOfPlayer = false;
            player = null;
        }
    }

    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
    }

    private void OnInDialogueUpdated()
    {
        if (InDialogue.Value == false && canMove == false)
        {
            SetCanMove(true);
        }
    }

    private void OnRPGEventTriggeredHandler(RPGEvent rpgEvent)
    {
        Dialogue dialogue = rpgEvent as Dialogue;

        if (dialogue != null)
        {
            if (DialogueBark != null)
            {
                DialogueBark.SetActive(false);
            }
            return;
        }

        Quest quest = rpgEvent as Quest;

        if (quest != null)
        {
            if (QuestBark != null)
            {
                QuestBark.SetActive(false);
            }
            return;
        }
    }

    private void OnRPGEventCompletedHandler(RPGEvent rpgEvent)
    {
        Quest quest = rpgEvent as Quest;

        if (quest != null)
        {
            if (QuestBark != null)
            {
                QuestBark.SetActive(true);
            }
        }
    }

    private void OnRPGEventClosedHandler(RPGEvent rpgEvent)
    {
        Quest quest = rpgEvent as Quest;

        if (quest != null)
        {
            if (QuestBark != null)
            {
                QuestBark.SetActive(false);
            }
        }
        
        EventTrigger.SetTriggered(false);
        UpdateRPGEvent();
    }

    public bool InRangeOfPlayer()
    {
        return inRangeOfPlayer;
    }

    private void UpdateRPGEvent()
    {
        if (EventTrigger.HasDialogue())
        {
            if (DialogueBark != null)
            {
                DialogueBark.SetActive(true);
            }

            EventTrigger.OnRPGEventTriggered -= OnRPGEventTriggeredHandler;
            EventTrigger.OnRPGEventTriggered += OnRPGEventTriggeredHandler;

            EventTrigger.OnRPGEventClosed -= OnRPGEventClosedHandler;
            EventTrigger.OnRPGEventClosed += OnRPGEventClosedHandler;
        }

        if (EventTrigger.HasQuest())
        {
            if (QuestBark != null)
            {
                QuestBark.SetActive(true);
            }

            EventTrigger.OnRPGEventTriggered -= OnRPGEventTriggeredHandler;
            EventTrigger.OnRPGEventTriggered += OnRPGEventTriggeredHandler;

            EventTrigger.OnRPGEventCompleted -= OnRPGEventCompletedHandler;
            EventTrigger.OnRPGEventCompleted += OnRPGEventCompletedHandler;

            EventTrigger.OnRPGEventClosed -= OnRPGEventClosedHandler;
            EventTrigger.OnRPGEventClosed += OnRPGEventClosedHandler;
        }
    }
    
    public void SetPhysics(bool usePhysics)
    {
        this.usePhysics = usePhysics;
    }

    private void OnPickupHandler()
    {
        if (inRangeOfPlayer == true && player != null)
        {
            if (EventTrigger != null && EventTrigger.IsTriggered() == false)
            {
                EventTrigger.SetTriggered(true);

                if (EventTrigger.HasDialogue())
                {
                    SetCanMove(false);
                    if (player != null)
                    {
                        player.SetCanMove(false);
                    }
                }

                bool result = EventTrigger.TriggerRPGEvent();
                if (result == false)
                {
                    DebugLog.Print(DebugLog.LogType.Warning, "cannot triiger event");
                    EventTrigger.SetTriggered(false);
                }
            }

            if (EventTrigger != null && EventTrigger.IsTriggered() == true)
            {
                if (EventTrigger.HasOpenQuest() == true)
                {
                    EventTrigger.TryCloseQuest();
                }
            }
        }
	}

	IEnumerator Jumping()
	{
		float startTime = Time.time;
		float y = transform.position.y;
		isJumping = true;
		while (Time.time - startTime <= JumpTime / 2)
		{
			transform.position += Vector3.up * JumpSpeed * Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		while (Time.time - startTime <= JumpTime)
		{
			transform.position -= Vector3.up * JumpSpeed * Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		transform.position = new Vector3(transform.position.x, y, transform.position.z);
		isJumping = false;
		jumpingCoroutine = null;
	}

    protected void Jump()
    {
        if (jumpingCoroutine == null)
        {
			jumpingCoroutine = StartCoroutine(Jumping());
		}
    }
}
