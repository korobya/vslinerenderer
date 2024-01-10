#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform vec4 color;

void main()
{
    gl_FragColor = color;
}