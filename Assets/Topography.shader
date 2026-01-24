Shader "Custom/Topography"
{
    Properties
    {
        [Header(Height Mapping)]
        _HeightMin ("Height Min", Float) = 0.0
        _HeightMax ("Height Max", Float) = 5.0
        _ColorRamp ("Color Ramp", 2D) = "white" {}

        [Header(Contour Lines)]
        _ContourInterval ("Contour Interval", Float) = 0.5
        _ContourThickness ("Contour Thickness", Float) = 0.02
        _ContourColor ("Contour Color", Color) = (0.3, 0.3, 0.3, 1)
        _DiscreteBands ("Use Discrete Bands", Float) = 0

        [Header(Sand Textures)]
        _SandTex ("Sand Albedo", 2D) = "white" {}
        _SandNormal ("Sand Normal", 2D) = "bump" {}
        _SandRoughness ("Sand Roughness", 2D) = "white" {}
        _SandOcclusion ("Sand AO", 2D) = "white" {}
        _SandScale ("Sand Scale", Float) = 10.0
        _SandColor ("Sand Tint", Color) = (1, 1, 1, 1)

        [Header(Sparkles)]
        _SparkleScale ("Sparkle Scale", Float) = 20.0
        _SparkleThreshold ("Sparkle Threshold", Range(0.8, 1.0)) = 0.95
        _SparkleIntensity ("Sparkle Intensity", Float) = 2.0

        [Header(Water Settings)]
        _WaterLevel ("Water Level", Float) = 0.5
        _WaterColor ("Water Color", Color) = (0, 0.2, 1, 1)
        _WaterOpacity ("Water Opacity", Range(0, 1)) = 0.6
        
        [Header(Water Visuals)]
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveFreq ("Wave Frequency", Float) = 2.0
        _Glossiness ("Water Glossiness", Float) = 100.0
        _FresnelPower ("Fresnel Power", Float) = 5.0
        _SpecularColor ("Specular Color", Color) = (0.2, 0.2, 0.2, 1)
        
        _Brightness ("Brightness", Float) = 1.0
        
        [Header(Color Blending)]
        _TintStrength ("Gradient Tint Strength", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv2          : TEXCOORD1; // Channel for Height Data
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv2          : TEXCOORD2;
            };

            // SRP Batcher Compatibility (UnityPerMaterial)
            CBUFFER_START(UnityPerMaterial)
                float _HeightMin;
                float _HeightMax;
                float _ContourInterval;
                float _ContourThickness;
                half4 _ContourColor;
                float _DiscreteBands;
                
                float _WaterLevel;
                float4 _WaterColor;
                float _WaterOpacity;

                float _WaveSpeed;
                float _WaveFreq;
                float _Glossiness;
                float _FresnelPower;
                float4 _SpecularColor;
                
                float4 _ColorRamp_ST;
                float _Brightness;

                float _SandScale;
                float4 _SandColor;
                float _SparkleScale;
                float _SparkleThreshold;
                float _SparkleIntensity;
                
                float _TintStrength;
                float _ColorShift;
            CBUFFER_END

            TEXTURE2D(_ColorRamp);
            SAMPLER(sampler_ColorRamp);

            TEXTURE2D(_SandTex);
            TEXTURE2D(_SandNormal);
            TEXTURE2D(_SandRoughness);
            TEXTURE2D(_SandOcclusion);
            SAMPLER(sampler_SandTex); // Shared sampler

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);
                
                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.uv2 = IN.uv2; // Pass height data
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Use UV2.x as the robust height signal (works even if mesh is flat)
                // Fallback to World Y if UV2 is empty (optional, but we'll assume script sets it)
                float height = IN.uv2.x; 

                float3 viewDir = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                // For lighting, we really want the "Flat" normal if in flat mode, 
                // OR the "Precomputed" normal if we want fake 3D lighting on a flat plane.
                // For now, let's just use the physical normal. 
                // Note: In flat mode, physical normal is (0,1,0), so lighting will look flat.
                // That's acceptable for a "projector" view.
                float3 normal = normalize(IN.normalWS);
                
                // --- 1. TRIPLANAR MAPPING (SAND) ---
                // World Space Position and Normal
                float3 worldPos = IN.positionWS * _SandScale;
                float3 absNormal = abs(normal);

                // Weights (Sharp blending via pow 8)
                float3 weights = pow(absNormal, 8.0);
                weights = weights / (weights.x + weights.y + weights.z);

                // Conditional sampling (Optimization optional, but we do full for quality)
                
                // Front (XY)
                float2 uvXY = worldPos.xy;
                half4 colXY = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uvXY);
                half4 nrmXY = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandTex, uvXY);
                half4 rghXY = SAMPLE_TEXTURE2D(_SandRoughness, sampler_SandTex, uvXY);
                half4 occXY = SAMPLE_TEXTURE2D(_SandOcclusion, sampler_SandTex, uvXY);

                // Top (XZ)
                float2 uvXZ = worldPos.xz;
                half4 colXZ = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uvXZ);
                half4 nrmXZ = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandTex, uvXZ);
                half4 rghXZ = SAMPLE_TEXTURE2D(_SandRoughness, sampler_SandTex, uvXZ);
                half4 occXZ = SAMPLE_TEXTURE2D(_SandOcclusion, sampler_SandTex, uvXZ);

                // Side (YZ)
                float2 uvYZ = worldPos.zy; // Swizzled ZY to avoid rotation
                half4 colYZ = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uvYZ);
                half4 nrmYZ = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandTex, uvYZ);
                half4 rghYZ = SAMPLE_TEXTURE2D(_SandRoughness, sampler_SandTex, uvYZ);
                half4 occYZ = SAMPLE_TEXTURE2D(_SandOcclusion, sampler_SandTex, uvYZ);

                // Blend
                half4 sandAlbedo = colXY * weights.z + colXZ * weights.y + colYZ * weights.x;
                half4 sandNormal = nrmXY * weights.z + nrmXZ * weights.y + nrmYZ * weights.x;
                half4 sandRough  = rghXY * weights.z + rghXZ * weights.y + rghYZ * weights.x;
                half4 sandAO     = occXY * weights.z + occXZ * weights.y + occYZ * weights.x;

                // Apply AO and Tint
                sandAlbedo *= sandAO;
                sandAlbedo *= _SandColor;

                // --- SPARKLES (Glitter) ---
                // Mask by Low Roughness (Smooth areas = wet/crystalline = sparkle)
                // Inverted Roughness map can act as Sparkle Mask
                float sparkleMask = 1.0 - sandRough.r;
                if (sparkleMask > _SparkleThreshold)
                {
                    // View-Dependent Sparkling
                    // Ideally we'd use Blue Noise, but here we use Roughness high-freq details or procedural noise
                    // A simple pseudo-random flicker based on ViewDir and Position
                    float3 glitterVec = floor(IN.positionWS * _SparkleScale);
                    float noise = frac(sin(dot(glitterVec, float3(12.9898, 78.233, 45.164))) * 43758.5453);
                    
                    float viewDependence = dot(normalize(glitterVec), viewDir); // Fake facet alignment
                    if (abs(viewDependence) > 0.9 && noise > 0.5)
                    {
                        sandAlbedo += _SparkleIntensity * (sparkleMask - _SparkleThreshold);
                    }
                }

                // --- COLOR MAPPING MIX ---
                // Map height to 0-1 with noise suppression at low scales
                float range = max(0.01, _HeightMax - _HeightMin);
                float height01 = saturate(((height - _HeightMin) / range) + _ColorShift);
                
                // Fade out the gradient colors if the world is "Flat" (Hill Height near 0)
                // This prevents tiny sensor noise from looking like huge mountains.
                float scaleFade = saturate(_HeightMax * 2.0); 
                height01 *= scaleFade; 
                
                // Sample gradient
                
                // Discrete bands mode: posterize height into bands aligned with contours
                float sampleHeight = height01;
                if (_DiscreteBands > 0.5) {
                    // Calculate which contour interval we're in
                    float stepIndex = floor(height / _ContourInterval);
                    // Determine the world height of that step
                    float steppedHeight = stepIndex * _ContourInterval;

                    // Normalize this stepped height to get the UV for the color ramp
                    sampleHeight = saturate(((steppedHeight - _HeightMin) / range) + _ColorShift);
                    
                    // Apply the same flat-mode fade out logic 
                    sampleHeight *= scaleFade;
                }
                
                half4 gradientColor = SAMPLE_TEXTURE2D(_ColorRamp, sampler_ColorRamp, float2(sampleHeight, 0.5));
                
                // SOFT TINT BLEND: Prevents "Muddy Green" artifacts
                // Boost brightness by 2.0 to counteract Multiply darkening (standard "Modulate" behavior)
                half4 tintedSand = sandAlbedo * gradientColor * 2.0;
                half4 terrainColor = lerp(sandAlbedo, tintedSand, _TintStrength);
                
                // Preserve Gradient Alpha (usually 1)
                terrainColor.a = 1.0;
                
                // --- 2. Contour Lines ---
                // We utilize fwidth/ddx for consistent visual thickness regardless of slope
                
                // Calculate grid position
                float contourGrid = height / _ContourInterval;
                
                // Pixel width of the gradient (gradient magnitude)
                float fw = max(0.0001, fwidth(contourGrid));
                
                // Distance to nearest integer line, normalized by the local gradient magnitude
                float distToLine = (min(frac(contourGrid), 1.0 - frac(contourGrid))) / fw;
                
                // Calculate line strength using a pixel-based thickness
                // 0.5 is a good baseline for smooth antialiased lines
                float lineStrength = smoothstep(_ContourThickness, 0.0, distToLine);
                
                // Apply Contour
                terrainColor = lerp(terrainColor, _ContourColor, lineStrength);


                // --- 3. Water Logic ---
                // If under water level, blend water color and add specular highlights
                if (height < _WaterLevel)
                {
                    // Water Surface Normal
                    // UC Davis creates a normal from water texture gradient. We don't have a fluid sim texture.
                    // We simulate "waviness" by perturbing the normal with noise/sin waves as in original Unity shader.
                    float wave = sin(IN.positionWS.x * _WaveFreq + _Time.y * _WaveSpeed) 
                               + sin(IN.positionWS.z * _WaveFreq * 0.8 + _Time.y * _WaveSpeed * 1.2);
                    
                    float3 waveNormalOffset = float3(wave * 0.1, 1.0, wave * 0.1); // Small perturbation
                    float3 wetNormal = normalize(normal + waveNormalOffset * 0.2); 

                    // Specular (High Glossiness like UC Davis)
                    float3 lightDir = normalize(float3(0.5, 1.0, -0.5)); // Fixed directional light
                    float3 halfVector = normalize(lightDir + viewDir);
                    float NdotH = max(0, dot(wetNormal, halfVector));
                    float specular = pow(NdotH, _Glossiness);
                    
                    // Fresnel
                    // UC Davis mostly uses spec, assuming "shiny"
                    // We add Fresnel for viewing angle realism
                    float fresnel = pow(1.0 - saturate(dot(wetNormal, viewDir)), _FresnelPower);
                    
                    // Water Color Blending
                    // UC Davis: baseColor = mix(baseColor, waterColor, min(waterLevel * waterOpacity, 1.0))
                    // Here waterLevel is (height - actual_water_height)? No, depth.
                    float waterDepth = _WaterLevel - height;
                    float opacity = saturate(waterDepth * _WaterOpacity); // Deeper = more opaque
                    
                    half4 waterFinal = _WaterColor + (specular * _SpecularColor) + (fresnel * 0.5 * _WaterColor);
                    
                    terrainColor = lerp(terrainColor, waterFinal, opacity);
                }

                // --- 4. Brightness Adjustment (Wall Dimming) ---
                terrainColor.rgb *= _Brightness;

                return terrainColor;
            }
            ENDHLSL
        }
    }
}
