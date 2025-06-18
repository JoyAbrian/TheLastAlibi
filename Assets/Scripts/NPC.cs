using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] private string NPCName;
    [SerializeField] private string personality;

    [Header("Reaction Sprites")]
    [SerializeField] private Sprite angrySprite;
    [SerializeField] private Sprite concernedSprite;
    [SerializeField] private Sprite happySprite;
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite smileSprite;
}