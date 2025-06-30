using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerModel", menuName = "Scriptable Objects/PlayerModel")]
public class PlayerModel : ScriptableObject
{
    public short MaxHealth = 0;
    public short CurrentHealth = 0;
    public short Damage = 0;
    public short MovementSpeed = 0;

    [Header("Dash Settings")]
    [SerializeField] public float dashRadius = 5f; // Radius to detect enemies
    [SerializeField] public float dashSpeed = 15f; // How fast the dash is
    [SerializeField] public float dashDuration = 0.3f; // How long the dash lasts
    [SerializeField] public float dashCooldown = 1f; // Cooldown between dashes
    [SerializeField] public float dashOffset = 1f; // Offset for dash

    [Header("Automatic Dash")]
    [SerializeField] public bool useAutomaticDash = true; // Whether to use automatic dashing
    [SerializeField] public float automaticDashInterval = 1f; // Time between automatic dashes


}
