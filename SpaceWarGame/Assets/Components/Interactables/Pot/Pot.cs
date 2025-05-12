using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pot : MonoBehaviour, IInteractable
{
    private const float MINIMUN_THROWING_POWER = 0.1f;
    
    [SerializeField] private float maxThrowingDistance = 5;
    [SerializeField] private float secondsTillFullPower = 3;
    [SerializeField] private Vector2 minToMaxTimeInAir = new Vector2(1, 2);
    
    [Header("Throwing Arc")]
    [SerializeField, Range(0, 1)] private float startTangentAverage = 0.5f;
    [SerializeField] private float startTangentUpMultipler = 0.5f;
    [SerializeField, Range(0, 1)] private float startTangentMinUpMultiplier = 0.5f;
    [SerializeField, Range(0, 1)] private float endTangentAverage = 0.5f;
    [SerializeField] private float endTangentUpMultipler = 0.5f;
    [SerializeField, Range(0, 1)] private float endTangentMinUpMultiplier = 0.5f;
    [SerializeField] private AnimationCurve inAirSpeed;
    
    [Header("Debug")]
    [SerializeField, Range(MINIMUN_THROWING_POWER, 1)] private float debugThrowStrength = 0.5f;

    private bool throwing;
    private float throwingPower = MINIMUN_THROWING_POWER;
    private float startTime;
    private float flyingTimeMax;
    private float flyingTime01;
    private Vector3 startPos;
    private Vector3 landingPos;
    private Vector3 startTangent;
    private Vector3 endTangent;

    #region MyRegion
    private void OnDrawGizmosSelected()
    {
        Vector3 startPos = transform.position;
        Vector3 landingPos = transform.position + transform.parent.forward * debugThrowStrength * maxThrowingDistance;
        landingPos.y = 0;
        Vector3 startTangent = Vector3.Lerp(startPos, landingPos, startTangentAverage) + Vector3.up * Mathf.Max(debugThrowStrength * startTangentUpMultipler, startTangentMinUpMultiplier);
        Vector3 endTangent = Vector3.Lerp(startPos, landingPos, endTangentAverage) + Vector3.up * Mathf.Max(debugThrowStrength * endTangentUpMultipler, endTangentMinUpMultiplier);
        
        Handles.DrawBezier(startPos, landingPos, startTangent, endTangent, Color.red, Texture2D.whiteTexture, 1f);
    }
    #endregion
    
    public void PickUp()
    {
    }
    public void StartUse()
    {
        startTime = Time.timeSinceLevelLoad;
    }
    public void Using()
    {
        throwingPower = Mathf.Lerp(MINIMUN_THROWING_POWER, 1, (Time.timeSinceLevelLoad - startTime) / 3);
    }
    public void UseDone(InputAction.CallbackContext ctx)
    {
        throwing = true;
        flyingTimeMax = Mathf.Lerp(minToMaxTimeInAir.x, minToMaxTimeInAir.y, throwingPower);
        
        startPos = transform.position;
        landingPos = transform.position + transform.parent.forward * throwingPower * maxThrowingDistance;
        landingPos.y = 0;
        startTangent = Vector3.Lerp(startPos, landingPos, startTangentAverage) + Vector3.up * Mathf.Max(throwingPower * startTangentUpMultipler, startTangentMinUpMultiplier);
        endTangent = Vector3.Lerp(startPos, landingPos, endTangentAverage) + Vector3.up * Mathf.Max(throwingPower * endTangentUpMultipler, endTangentMinUpMultiplier);
    }

    private void Update()
    {
        if(!throwing)
            return;

        flyingTime01 += Time.deltaTime / flyingTimeMax;

        transform.parent.position = HelperFunctinos.CubicBezier(startPos, landingPos, startTangent, endTangent, inAirSpeed.Evaluate(flyingTime01));

        if (flyingTime01 >= 1)
        {
            flyingTime01 = 0;
            throwing = false;
        }
            
    }
    public Transform GetTransform()
    {
        return transform.parent;
    }
}
