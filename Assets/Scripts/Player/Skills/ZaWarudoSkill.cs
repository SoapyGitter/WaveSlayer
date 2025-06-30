using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ZaWarudoSkill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDashAttack playerDashAttack;
    [SerializeField] private Volume postProcessingVolume;
    [SerializeField] private AudioClip zaWarudoSound;
    [SerializeField] private AudioClip timeStopSound;
    [SerializeField] private GameObject timeStopEffectPrefab;
    
    [Header("Visual Effect Settings")]
    [SerializeField] private float effectDuration = 5f; // How long the effect lasts
    [SerializeField] private float effectBuildupTime = 1.5f; // Time for the effect to reach peak intensity
    [SerializeField] private float flashTime = 1f; // Time for the flash effect
    [SerializeField] private float colorDrainSpeed = 2f; // How quickly colors drain for time stop effect
    [SerializeField] private Color timeStopVignetteColor = new Color(1f, 0.8f, 0.2f); // Gold/yellow color for vignette
    [SerializeField] private float flashColorDistortionIntensity = 1f; // Intensity of color distortion during flash
    [SerializeField] private float flashSaturationAmount = -80f; // Amount of saturation change during flash (-100 is completely B&W)
    
    // Post-processing effect components
    private DepthOfField depthOfField;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private ColorAdjustments colorAdjustments;
    private LensDistortion lensDistortion;
    
    // State
    private bool isEffectActive = false;
    private AudioSource audioSource;
    private CancellationTokenSource cts = new CancellationTokenSource();
    
    private void Awake()
    {
        // Find references if not set
        if (playerDashAttack == null)
        {
            playerDashAttack = GetComponentInParent<PlayerDashAttack>();
            if (playerDashAttack == null)
            {
                playerDashAttack = FindObjectOfType<PlayerDashAttack>();
            }
        }
        
        if (postProcessingVolume == null)
        {
            postProcessingVolume = FindObjectOfType<Volume>();
        }
        
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize post-processing effects
        InitializePostProcessingEffects();
    }
    
    private void OnDestroy()
    {
        // Cancel any running tasks when the object is destroyed
        cts.Cancel();
        cts.Dispose();
        
        // Make sure auto dash is disabled when object is destroyed
        if (playerDashAttack != null && isEffectActive)
        {
            playerDashAttack.ToggleAutomaticDash(false);
        }
    }
    
    // Initialize post-processing components for Za Warudo effect
    private void InitializePostProcessingEffects()
    {
        if (postProcessingVolume != null)
        {
            // Get or add depth of field effect
            if (!postProcessingVolume.profile.TryGet(out depthOfField))
            {
                depthOfField = postProcessingVolume.profile.Add<DepthOfField>();
            }
            
            // Get or add vignette effect
            if (!postProcessingVolume.profile.TryGet(out vignette))
            {
                vignette = postProcessingVolume.profile.Add<Vignette>();
            }
            
            // Get or add chromatic aberration effect
            if (!postProcessingVolume.profile.TryGet(out chromaticAberration))
            {
                chromaticAberration = postProcessingVolume.profile.Add<ChromaticAberration>();
            }
            
            // Get or add color adjustments effect (for desaturation)
            if (!postProcessingVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = postProcessingVolume.profile.Add<ColorAdjustments>();
            }
            
            // Get or add lens distortion effect
            if (!postProcessingVolume.profile.TryGet(out lensDistortion))
            {
                lensDistortion = postProcessingVolume.profile.Add<LensDistortion>();
            }
            
            // Initialize effects with zero intensity
            ResetEffects();
        }
        else
        {
            Debug.LogWarning("No Post-processing Volume found for Za Warudo effect!");
        }
    }
    
    // Reset effects to default values
    private void ResetEffects()
    {
        if (depthOfField != null)
        {
            depthOfField.active = true;
            depthOfField.focusDistance.Override(10f);
            depthOfField.focalLength.Override(0f);
        }
        
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.Override(0f);
            vignette.color.Override(timeStopVignetteColor);
        }
        
        if (chromaticAberration != null)
        {
            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(0f);
        }
        
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.saturation.Override(0f);
            colorAdjustments.contrast.Override(0f);
        }
        
        if (lensDistortion != null)
        {
            lensDistortion.active = true;
            lensDistortion.intensity.Override(0f);
        }
    }
    
    public void ActivateSkill()
    {
        ActivateZaWarudo();
    }
    
    // Public method to activate Za Warudo time stop
    private async UniTaskVoid ActivateZaWarudo()
    {
        if (!isEffectActive)
        {
            // Cancel any previous tasks
            cts.Cancel();
            cts = new CancellationTokenSource();
            
            // Execute the sequence
            await ZaWarudoSequence(cts.Token);
        }
    }
    
    // UniTask to handle the Za Warudo sequence
    private async UniTask ZaWarudoSequence(CancellationToken cancellationToken)
    {
        isEffectActive = true;
        
        // Play Za Warudo voice clip
        if (zaWarudoSound != null && audioSource != null)
        {
            audioSource.pitch = 1.0f; // Ensure normal pitch
            audioSource.PlayOneShot(zaWarudoSound);
        }
        
        // Initial flash effect - quick white out and back
        TimeStopFlashEffect(cancellationToken).Forget();
        
        // Short pause for dramatic effect after voice line
        await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: cancellationToken);
        
        // Create visual time stop effect at player position
        if (timeStopEffectPrefab != null && !cancellationToken.IsCancellationRequested)
        {
            Instantiate(timeStopEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Play time stop sound
        if (timeStopSound != null && audioSource != null && !cancellationToken.IsCancellationRequested)
        {
            audioSource.PlayOneShot(timeStopSound);
        }
        
        // Enable auto attack immediately when effect begins
        if (playerDashAttack != null && !cancellationToken.IsCancellationRequested)
        {
            playerDashAttack.ToggleAutomaticDash(true);
        }
        
        // Build up visual effect intensity over time
        float elapsedTime = 0f;
        while (elapsedTime < effectBuildupTime && !cancellationToken.IsCancellationRequested)
        {
            float progress = elapsedTime / effectBuildupTime;
            UpdateEffectIntensity(progress);
            
            await UniTask.DelayFrame(1, PlayerLoopTiming.Update, cancellationToken);
            elapsedTime += Time.unscaledDeltaTime;
        }
        
        if (cancellationToken.IsCancellationRequested)
        {
            ResetVisualEffects();
            return;
        }
        
        // Hold the effect at full intensity
        UpdateEffectIntensity(1.0f);
        
        // Wait for the full effect duration
        await UniTask.Delay((int)((effectDuration - effectBuildupTime) * 1000), ignoreTimeScale: true, cancellationToken: cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
        {
            ResetVisualEffects();
            return;
        }
        
        // Gradually fade out effects
        elapsedTime = 0f;
        float fadeOutTime = 1.0f;
        while (elapsedTime < fadeOutTime && !cancellationToken.IsCancellationRequested)
        {
            float progress = 1.0f - (elapsedTime / fadeOutTime);
            UpdateEffectIntensity(progress);
            
            await UniTask.DelayFrame(1, PlayerLoopTiming.Update, cancellationToken);
            elapsedTime += Time.unscaledDeltaTime;
        }
        
        // Reset only visual effects
        ResetVisualEffects();
    }
    
    private void ResetVisualEffects()
    {
        // Reset all effects
        ResetEffects();
        
        // Make sure to disable auto dash when effect ends
        if (playerDashAttack != null)
        {
            playerDashAttack.ToggleAutomaticDash(false);
        }
        
        isEffectActive = false;
    }
    
    // Update post-processing effect intensities based on progress (0-1)
    private void UpdateEffectIntensity(float progress)
    {
        if (postProcessingVolume == null) return;
        
        // Depth of field (blur effect)
        if (depthOfField != null)
        {
            depthOfField.focalLength.Override(10f * progress);
        }
        
        // Vignette
        if (vignette != null)
        {
            vignette.intensity.Override(0.5f * progress);
        }
        
        // Chromatic aberration
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.Override(0.8f * progress);
        }
        
        // Color adjustments (drain color for time stop effect)
        if (colorAdjustments != null)
        {
            // Reduce saturation (makes image more black and white)
            colorAdjustments.saturation.Override(-50f * progress);
            
            // Increase contrast for dramatic effect
            colorAdjustments.contrast.Override(20f * progress);
        }
    }
    
    // Create a flash effect for time stop using UniTask
    private async UniTaskVoid TimeStopFlashEffect(CancellationToken cancellationToken)
    {
        if (!postProcessingVolume.profile.TryGet(out lensDistortion))
        {
            lensDistortion = postProcessingVolume.profile.Add<LensDistortion>();
        }
        
        // Get temporary reference to chromatic aberration for color distortion
        ChromaticAberration flashChromaticAberration;
        if (!postProcessingVolume.profile.TryGet(out flashChromaticAberration))
        {
            flashChromaticAberration = postProcessingVolume.profile.Add<ChromaticAberration>();
        }
        
        // Get color adjustments for black and white effect
        ColorAdjustments flashColorAdjustments;
        if (!postProcessingVolume.profile.TryGet(out flashColorAdjustments))
        {
            flashColorAdjustments = postProcessingVolume.profile.Add<ColorAdjustments>();
        }
        
        // Store original values to restore later
        float originalChromaticValue = flashChromaticAberration.intensity.value;
        float originalSaturationValue = flashColorAdjustments.saturation.value;
        
        // Enable effects
        lensDistortion.active = true;
        flashChromaticAberration.active = true;
        flashColorAdjustments.active = true;
        
        // Flash effect with distortion and color aberration
        float elapsedTime = 0f;
        
        // Build up flash with color distortion
        while (elapsedTime < flashTime && !cancellationToken.IsCancellationRequested)
        {
            float progress = elapsedTime / flashTime;
            
            // Apply lens distortion
            lensDistortion.intensity.Override(progress);
            
            // Apply strong color distortion during flash
            flashChromaticAberration.intensity.Override(flashColorDistortionIntensity * progress);
            
            // Apply black and white effect during flash
            flashColorAdjustments.saturation.Override(flashSaturationAmount * progress);
            
            await UniTask.DelayFrame(1, PlayerLoopTiming.Update, cancellationToken);
            elapsedTime += Time.unscaledDeltaTime;
        }
        
        if (cancellationToken.IsCancellationRequested) 
        {
            // Reset effects if canceled
            lensDistortion.intensity.Override(0f);
            flashChromaticAberration.intensity.Override(originalChromaticValue);
            flashColorAdjustments.saturation.Override(originalSaturationValue);
            return;
        }
        
        // Hold briefly at peak
        await UniTask.Delay(50, ignoreTimeScale: true, cancellationToken: cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
        {
            // Reset effects if canceled
            lensDistortion.intensity.Override(0f);
            flashChromaticAberration.intensity.Override(originalChromaticValue);
            flashColorAdjustments.saturation.Override(originalSaturationValue);
            return;
        }
        
        // Fade out flash with color distortion
        elapsedTime = 0f;
        while (elapsedTime < flashTime && !cancellationToken.IsCancellationRequested)
        {
            float progress = 1.0f - (elapsedTime / flashTime);
            
            // Fade lens distortion
            lensDistortion.intensity.Override(0.5f * progress);
            
            // Fade color distortion
            flashChromaticAberration.intensity.Override(flashColorDistortionIntensity * progress);
            
            // Fade back from black and white
            flashColorAdjustments.saturation.Override(flashSaturationAmount * progress);
            
            await UniTask.DelayFrame(1, PlayerLoopTiming.Update, cancellationToken);
            elapsedTime += Time.unscaledDeltaTime;
        }
        
        // Reset lens distortion and chromatic aberration
        lensDistortion.intensity.Override(0f);
        
        // Don't fully reset chromatic aberration as it will be managed by the main effect
        // Just reset to its initial state
        flashChromaticAberration.intensity.Override(originalChromaticValue);
        
        // Reset color saturation
        flashColorAdjustments.saturation.Override(originalSaturationValue);
    }
    
    // Public method to check if Za Warudo is active
    public bool IsTimeStopActive()
    {
        return isEffectActive;
    }
}
