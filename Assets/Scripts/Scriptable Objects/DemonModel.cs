using UnityEngine;

[CreateAssetMenu(fileName = "NewDemonModel", menuName = "Scriptable Objects/DemonModel")]
public class DemonModel : ScriptableObject
{
    [Header("Basic Properties")]
    public string DemonName = "Unnamed Demon";
    public short MaxHealth = 10;
    public short CurrentHealth = 10;

    [Header("Combat Properties")]
    public short Damage = 1;
    public float AttackRange = 1.5f;
    public float AttackCooldown = 1.0f;

    [Header("Movement Properties")]
    public float MovementSpeed = 3.0f;
    public float DetectionRange = 10.0f;

    [Header("Reward Properties")]
    public int ScoreValue = 100;
    public int ExperienceValue = 10; // Experience points granted when defeating this demon
}