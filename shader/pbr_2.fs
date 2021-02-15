#version 330 core
out vec4 FragColor;
in vec2 TexCoords;
in vec3 WorldPos;
in vec3 Normal;

// material parameters
uniform vec3 albedo;
uniform float metallic;
uniform float roughness;
uniform float ao;

// IBL
uniform samplerCube irradianceMap;

// lights
uniform vec3 lightPositions[4];
uniform vec3 lightColors[4];

uniform vec3 camPos;

const float PI = 3.14159265359;


// 菲涅尔方程：Fresnel-Schlick 计算被反射的光线对比光线被折射部分所占的比率,这个比率会随着我们观察的角度不同而不同
// parmas NdotV : 表面法向量N与观察方向V的点乘结果
// params F0       ： 平面的基础反射率
vec3 F_FS(float NdotV,vec3 F0)
{
    return F0 + (1.0 - F0) * pow((1.0 - NdotV),5.0);
}

// 正态分布： Trowbridge-ReitzGGX  估算在受到表面粗糙度的影响下，取向方向与中间向量一致的微平面的数量。
//           如果我们的微平面中有35%与向量h取向一致，则正态分布函数或者说NDF将会返回0.35
// params N             :表面法向量
// params H             :half vector
// parmas roughness     :粗糙度
float D_GGX_TR(vec3 N,vec3 H,float roughness)
{
    float a  = roughness*roughness;
    float a2 = a * a;
    float NdotH = max(dot(N,H),0.0);
    float NdotH2 = NdotH*NdotH;

    float nom = a2;
    float denom =  NdotH2 * (a2 - 1.0) + 1.0; 
    denom = PI * denom * denom;
    return nom / max(denom,0.0000001); // 避免当 roughness=0 且 NdotH=1.0  时，除 0 
}

// 几何函数： 几何函数从统计学上近似的求得了微平面间相互遮蔽的比率，这种相互遮蔽会损耗光线的能量。
// params NDotV       : 表面法向量N与观察方向V的点乘结果
// params roughness   : 粗糙度
float G_GGX_S(float NdotV,float roughness)
{
    float r = roughness + 1.0;
    // 这里的k是α基于几何函数是针对直接光照还是针对IBL光照的重映射(Remapping) 直接光照和间接光照有所不同
    float k = (r*r) / 8.0; // 直接光照情况下
    float nom = NdotV;
    float denom = NdotV * (1.0 - k) + k;
    return nom / denom;
}

// 为了有效的估算几何部分，需要将观察方向（几何遮蔽(Geometry Obstruction)）和光线方向向量（几何阴影(Geometry Shadowing)）都考虑进去。
// 我们可以使用史密斯法(Smith’s method)来把两者都纳入其中
// params N     : 表面法向量
// params V     : 观察方向
// params L     : 光源方向
float G_GGX_Smith(vec3 N,vec3 V,vec3 L,float roughness)
{
    float NdotV = max(dot(N,V),0.0);
    float NDotL = max(dot(N,L),0.0);
    float ggx2 = G_GGX_S(NdotV,roughness);
    float ggx1 = G_GGX_S(NdotV,roughness);
    return ggx1 * ggx2;
}







// ----------------------------------------------------------------------------
void main()
{		

    vec3 N = normalize(Normal);
    vec3 V = normalize(camPos - WorldPos);
    vec3 F0 = vec3(0.04); // 0.04 大多数电介质表面而言使用 0.04 作为基础反射率已经足够好了
    F0 = mix(F0,albedo,metallic);
    vec3 Lo = vec3(0.0);
    for(int i = 0;i<4;i++)
    {
        vec3 L = normalize(lightPositions[i] - WorldPos);
        vec3 H = normalize(V + L);
           
        float r = length(lightPositions[i] - WorldPos);
        float attenuation = 1.0 / (r * r);   // 衰减
        vec3 radiance = lightColors[i] * attenuation;

        // Cook-Torrance specular BRDF


        float D = D_GGX_TR(N,H,roughness); // 正态分布
        vec3 F = F_FS(max(dot(H,V),0.0),F0); // 菲涅尔 （高光部分）
        float G = G_GGX_Smith(N,V,L,roughness); // 几何遮蔽

        float VdotN = max(dot(V,N),0);
        float NdotL = max(dot(L,N),0);

        vec3 specular = (D*F*G) / (4.0*VdotN*NdotL + 0.001); // 加 0.001 避免除 0
        
        vec3 kS = F; // 镜面反射比例其实就是F
        vec3 kD = vec3(1.0) - kS;

        kD *= 1.0 - metallic; //金属不会折射光线，因此没有漫反射。 所以如果表面是金属的 我们会把系数kD变为0

        Lo += (kD*albedo/PI + specular) * radiance * NdotL;
    }

    // 环境光 （IBL）
    vec3 kS = F_FS(max(dot(N, V), 0.0), F0);
    vec3 kD = 1.0 - kS;
    kD *= 1.0 - metallic;
    vec3 irradiance = texture(irradianceMap, N).rgb;
    vec3 diffuse      = irradiance * albedo;
    vec3 ambient = (kD * diffuse) * ao;


    vec3 color = ambient + Lo;

    // Tonemapping (Reinhard)
    color = color / (color + vec3(1.0));

    // Gamma Correction
    color = pow(color,vec3(1.0/2.2));

    FragColor = vec4(color,1.0);
}