using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Pirate Bombs/Level Data")]
public class LevelData : ScriptableObject
{
    public int levelNumber;
    public string displayName;
    public string sceneName;
    public int enemyCount;
    public PlayerMovementBalance playerMovement;
    public PlayerHealthBalance playerHealth;
    public PlayerBombBalance playerBomb;
    public EnemyHealthBalance enemyHealth;
    public Enemy1_KickerBalance enemy1_Kicker;
    public EnemyBehaviorAIBalance enemyBehaviorAI;
    public BombBalance bomb;
    public ItemPhysicsBalance itemPhysics;

    public void Apply(PlayerMovement target)
    {
        target.moveSpeed = playerMovement.moveSpeed;
        target.jumpForce = playerMovement.jumpForce;
        target.groundCheckRadius = playerMovement.groundCheckRadius;
        target.landingDuration = playerMovement.landingDuration;
    }

    public void Apply(PlayerHealth target)
    {
        target.maxHealth = playerHealth.maxHealth;
        target.knockbackForce = playerHealth.knockbackForce;
        target.knockbackUpForce = playerHealth.knockbackUpForce;
        target.hitDuration = playerHealth.hitDuration;
    }

    public void Apply(PlayerBomb target)
    {
        target.holdThreshold = playerBomb.holdThreshold;
        target.maxChargeTime = playerBomb.maxChargeTime;
        target.minThrowForce = playerBomb.minThrowForce;
        target.maxThrowForce = playerBomb.maxThrowForce;
        target.throwUpForce = playerBomb.throwUpForce;
        target.placementDistance = playerBomb.placementDistance;
    }

    public void Apply(EnemyHealth target)
    {
        target.maxHealth = enemyHealth.maxHealth;
        target.knockbackForce = enemyHealth.knockbackForce;
        target.knockbackUpForce = enemyHealth.knockbackUpForce;
        target.groundCheckRadius = enemyHealth.groundCheckRadius;
        target.hitDuration = enemyHealth.hitDuration;
        target.landingTimeout = enemyHealth.landingTimeout;
        target.destroyAfterDeath = enemyHealth.destroyAfterDeath;
    }

    public void Apply(Enemy1_Kicker target)
    {
        target.detectionRadius = enemy1_Kicker.detectionRadius;
        target.attackRange = enemy1_Kicker.attackRange;
        target.attackReachTolerance = enemy1_Kicker.attackReachTolerance;
        target.moveSpeed = enemy1_Kicker.moveSpeed;
        target.groundCheckRadius = enemy1_Kicker.groundCheckRadius;
        target.jumpForce = enemy1_Kicker.jumpForce;
        target.jumpHeightThreshold = enemy1_Kicker.jumpHeightThreshold;
        target.jumpHorizontalRange = enemy1_Kicker.jumpHorizontalRange;
        target.jumpCooldown = enemy1_Kicker.jumpCooldown;
        target.obstacleCheckDistance = enemy1_Kicker.obstacleCheckDistance;
        target.obstacleCheckHeight = enemy1_Kicker.obstacleCheckHeight;
        target.attackDamage = enemy1_Kicker.attackDamage;
        target.bombKickForce = enemy1_Kicker.bombKickForce;
        target.bombKickUpForce = enemy1_Kicker.bombKickUpForce;
        target.attackHitTime = enemy1_Kicker.attackHitTime;
        target.attackDuration = enemy1_Kicker.attackDuration;
        target.attackCooldown = enemy1_Kicker.attackCooldown;
        target.restAfterBombKick = enemy1_Kicker.restAfterBombKick;
        target.questionDuration = enemy1_Kicker.questionDuration;
        target.alertDuration = enemy1_Kicker.alertDuration;
    }

    public void Apply(EnemyBehaviorAI target)
    {
        target.detectionRadius = enemyBehaviorAI.detectionRadius;
        target.attackRange = enemyBehaviorAI.attackRange;
        target.attackReachTolerance = enemyBehaviorAI.attackReachTolerance;
        target.moveSpeed = enemyBehaviorAI.moveSpeed;
        target.groundCheckRadius = enemyBehaviorAI.groundCheckRadius;
        target.jumpForce = enemyBehaviorAI.jumpForce;
        target.jumpHeightThreshold = enemyBehaviorAI.jumpHeightThreshold;
        target.jumpHorizontalRange = enemyBehaviorAI.jumpHorizontalRange;
        target.jumpCooldown = enemyBehaviorAI.jumpCooldown;
        target.obstacleCheckDistance = enemyBehaviorAI.obstacleCheckDistance;
        target.obstacleCheckHeight = enemyBehaviorAI.obstacleCheckHeight;
        target.attackDamage = enemyBehaviorAI.attackDamage;
        target.attackHitTime = enemyBehaviorAI.attackHitTime;
        target.attackDuration = enemyBehaviorAI.attackDuration;
        target.attackCooldown = enemyBehaviorAI.attackCooldown;
        target.restAfterBomb = enemyBehaviorAI.restAfterBomb;
        target.fleeDistance = enemyBehaviorAI.fleeDistance;
        target.fleeSafetyMargin = enemyBehaviorAI.fleeSafetyMargin;
        target.restAfterFlee = enemyBehaviorAI.restAfterFlee;
        target.questionDuration = enemyBehaviorAI.questionDuration;
        target.alertDuration = enemyBehaviorAI.alertDuration;
    }

    public void Apply(Bomb target)
    {
        target.explosionDelay = bomb.explosionDelay;
        target.explosionRadius = bomb.explosionRadius;
        target.damage = bomb.damage;
        target.ignitionDelay = bomb.ignitionDelay;
        target.extinguishDuration = bomb.extinguishDuration;
    }

    public void Apply(ItemPhysics target)
    {
        target.explosionForce = itemPhysics.explosionForce;
        target.upwardForce = itemPhysics.upwardForce;
        target.rotationForce = itemPhysics.rotationForce;
    }
}

[Serializable]
public class PlayerMovementBalance
{
    public float moveSpeed;
    public float jumpForce;
    public float groundCheckRadius;
    public float landingDuration;
}

[Serializable]
public class PlayerHealthBalance
{
    public int maxHealth;
    public float knockbackForce;
    public float knockbackUpForce;
    public float hitDuration;
}

[Serializable]
public class PlayerBombBalance
{
    public float holdThreshold;
    public float maxChargeTime;
    public float minThrowForce;
    public float maxThrowForce;
    public float throwUpForce;
    public float placementDistance;
}

[Serializable]
public class EnemyHealthBalance
{
    public int maxHealth;
    public float knockbackForce;
    public float knockbackUpForce;
    public float groundCheckRadius;
    public float hitDuration;
    public float landingTimeout;
    public float destroyAfterDeath;
}

[Serializable]
public class Enemy1_KickerBalance
{
    public float detectionRadius;
    public float attackRange;
    public float attackReachTolerance;
    public float moveSpeed;
    public float groundCheckRadius;
    public float jumpForce;
    public float jumpHeightThreshold;
    public float jumpHorizontalRange;
    public float jumpCooldown;
    public float obstacleCheckDistance;
    public float obstacleCheckHeight;
    public int attackDamage;
    public float bombKickForce;
    public float bombKickUpForce;
    public float attackHitTime;
    public float attackDuration;
    public float attackCooldown;
    public float restAfterBombKick;
    public float questionDuration;
    public float alertDuration;
}

[Serializable]
public class EnemyBehaviorAIBalance
{
    public float detectionRadius;
    public float attackRange;
    public float attackReachTolerance;
    public float moveSpeed;
    public float groundCheckRadius;
    public float jumpForce;
    public float jumpHeightThreshold;
    public float jumpHorizontalRange;
    public float jumpCooldown;
    public float obstacleCheckDistance;
    public float obstacleCheckHeight;
    public int attackDamage;
    public float attackHitTime;
    public float attackDuration;
    public float attackCooldown;
    public float restAfterBomb;
    public float fleeDistance;
    public float fleeSafetyMargin;
    public float restAfterFlee;
    public float questionDuration;
    public float alertDuration;
}

[Serializable]
public class BombBalance
{
    public float explosionDelay;
    public float explosionRadius;
    public int damage;
    public float ignitionDelay;
    public float extinguishDuration;
}

[Serializable]
public class ItemPhysicsBalance
{
    public float explosionForce;
    public float upwardForce;
    public float rotationForce;
}
