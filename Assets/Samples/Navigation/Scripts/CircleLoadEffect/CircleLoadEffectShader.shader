Shader"Custom/WaterFillMultiRadial"
{
    Properties
    {
        _MainTex        ("Main Texture", 2D) = "white" {}
        _Color          ("Color Tint",   Color) = (1,1,1,1)

        _FillHeight ("Fill Height (Y)", Float) = 0

        /* базовый горизонтальный фон */
        _LeftCol   ("Left Color",  Color) = (0.0,0.75,1.0,1)
        _RightCol  ("Right Color", Color) = (0.0,1.0,0.40,1)
        _ShiftSpeed ("Shift Speed", Float) = 0.2
        _BlendWidth ("Blend Width", Range(0,0.5)) = 0.04

        /* ——— GRADIENT #1 (жёлто‑зелёное пятно) ——— */
        _Rad1_Center ("Rad1 Center (local XY)", Vector) = ( 0.8, 0.9, 0, 0)
        _Rad1_Radius ("Rad1 Radius",  Float)   = 0.45
        _Rad1_Col    ("Rad1 Color",   Color)   = (1.0,0.96,0.30,1)

        /* ——— GRADIENT #2 (голубое пятно) ——— */
        _Rad2_Center ("Rad2 Center (local XY)", Vector) = (0.15,-0.85,0,0)
        _Rad2_Radius ("Rad2 Radius",  Float)   = 0.30
        _Rad2_Col    ("Rad2 Color",   Color)   = (0.02,0.55,1.0,1)

        /* ——— GRADIENT #3 (салатовое пятно) ——— */
        _Rad3_Center ("Rad3 Center (local XY)", Vector) = ( 0.9, 0.9,0,0)
        _Rad3_Radius ("Rad3 Radius",  Float)   = 0.50
        _Rad3_Col    ("Rad3 Color",   Color)   = (0.50,1.0,0.35,1)

        [Toggle] _EnableWaves ("Enable Waves", Float) = 1
        _WaveStrength ("Wave Amp", Range(0,1)) = 0.25
        _WaveSpeed    ("Wave Speed", Float) = 1
        _WaveFrequency("Wave Freq", Float) = 3

        _EdgeSoftness ("Soft Edge", Range(0.001,1)) = 0.12
        _GlobalAlpha  ("Global Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
Blend SrcAlpha
OneMinusSrcAlpha
            ZWrite
Off
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
#include "UnityCG.cginc"

            /* uniforms */
sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Color;

float _FillHeight;
float4 _LeftCol, _RightCol;
float _ShiftSpeed, _BlendWidth;

float4 _Rad1_Center, _Rad1_Col;
float _Rad1_Radius;
float4 _Rad2_Center, _Rad2_Col;
float _Rad2_Radius;
float4 _Rad3_Center, _Rad3_Col;
float _Rad3_Radius;

float _EnableWaves, _WaveStrength, _WaveSpeed, _WaveFrequency;
float _EdgeSoftness, _GlobalAlpha;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};
struct v2f
{
    float2 uv : TEXCOORD0;
    float3 localPos : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float4 clip : SV_POSITION;
};

v2f vert(appdata v)
{
    v2f o;
    o.clip = UnityObjectToClipPos(v.vertex);
    o.localPos = v.vertex.xyz;
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

            /*— удобный хелпер: гладкая «колокол»‑маска радиального пятна —*/
float radialMask(float2 p, float2 c, float r)
{
    float d = distance(p, c) / r; // 1 на границе
    return saturate(1 - d * d); // плавный спад к краю
}

fixed4 frag(v2f i) : SV_Target
{
    fixed4 tex = tex2D(_MainTex, i.uv) * _Color;

                /* волны */
    float wave = _EnableWaves * sin((i.worldPos.x + i.worldPos.z) * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveStrength;
    float depth = (_FillHeight + wave) - i.localPos.y;

                /* базовый горизонтальный градиент */
    float phase = frac(i.uv.x + _Time.y * _ShiftSpeed);
    float tBase = 0.5 - 0.5 * cos(2 * UNITY_PI * phase);
    float t = saturate(lerp(tBase, step(0.5, tBase), _BlendWidth));
    fixed3 col = lerp(_LeftCol.rgb, _RightCol.rgb, t);

                /* координаты в локальной плоскости X‑Y (нормируем к [-1,1]) */
    float2 p = i.localPos.xy; // сфера радиус ≈1
                /* — добавляем 3 «пятна» — */
    col += _Rad1_Col.rgb * radialMask(p, _Rad1_Center.xy, _Rad1_Radius);
    col += _Rad2_Col.rgb * radialMask(p, _Rad2_Center.xy, _Rad2_Radius);
    col += _Rad3_Col.rgb * radialMask(p, _Rad3_Center.xy, _Rad3_Radius);
    col = saturate(col); // защита от >1

    fixed4 waterCol = fixed4(col, 1);

    fixed4 baseCol = (depth > 0) ? waterCol : tex;
    float mask = saturate(1.0 - abs(depth) / _EdgeSoftness);
    fixed4 final = lerp(baseCol, waterCol, mask);
    final.a *= _GlobalAlpha;
    return final;
}
            ENDCG
        }
    }
Fallback"Diffuse"
}
