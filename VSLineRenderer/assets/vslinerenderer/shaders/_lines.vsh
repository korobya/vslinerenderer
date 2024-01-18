#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 quadPos;

layout(location = 2) in vec3 posA;
layout(location = 3) in vec3 posB;

uniform mat4 modelViewMatrix;
uniform mat4 projectionMatrix;

uniform vec2 resolution;

uniform float width;

void main()
{
    vec4 viewPosA = modelViewMatrix *  vec4(posA, 1.0);
    vec4 viewPosB = modelViewMatrix *  vec4(posB, 1.0);

    vec4 clipPosA = projectionMatrix * viewPosA;
    vec4 clipPosB = projectionMatrix * viewPosB;

    vec2 ndcPosA = resolution * (clipPosA.xy / clipPosA.w);
    vec2 ndcPosB = resolution * (clipPosB.xy / clipPosB.w);

    vec2 dir = normalize(ndcPosB - ndcPosA);
    vec2 normal = vec2(-dir.y, dir.x);

    vec2 ptA = ndcPosA + (width / -viewPosA.z) * quadPos.y * normal;
    vec2 ptB = ndcPosB + (width / -viewPosB.z) * quadPos.y * normal;

    vec2 pt = mix(ptA, ptB, quadPos.z);
    vec4 clipPos = mix(clipPosA, clipPosB, quadPos.z);

    gl_Position = vec4(clipPos.w * (pt / resolution), clipPos.z, clipPos.w);
}