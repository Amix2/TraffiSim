using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.Assertions;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class rgRoadMapFromSVG 
{

    class Circle
    {
        float2 position;
        float radius;

        public Circle(float2 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }
    }   

    List<Circle> circles = new List<Circle>();


    public rgRoadMapFromSVG(string svgFileName)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(svgFileName);
        ParseNodeList(xmlDoc.DocumentElement.ChildNodes, float4x4.identity);
    }

    void ParseNodeList(XmlNodeList nodeList, float4x4 transform)
    {
        foreach (XmlNode node in nodeList)
            ParseNode(node, transform);
    }

    void ParseNode(XmlNode node, float4x4 transform)
    {
        Debug.Log(node.Name);
        if (node.Attributes != null)
        {
            if (node.Attributes["transform"] != null)
            {
                float4x4 thisNodeTransform = FromTransform(node.Attributes["transform"].Value);
                transform = math.mul(transform, thisNodeTransform);
            }
        }
        switch (node.Name)
        {
            case "circle":
                {
                    Assert.IsNotNull(node.Attributes);
                    float cx = node.Attributes["cx"] != null ? float.Parse(node.Attributes["cx"].Value) : 0;
                    float cy = node.Attributes["cy"] != null ? float.Parse(node.Attributes["cy"].Value) : 0;
                    float r = node.Attributes["r"] != null ? float.Parse(node.Attributes["r"].Value) : 0;
                    float4 pos = new float4(cx, 0, cy, 1);
                    pos = math.mul(transform, pos);
                    float scale = math.cmax(transform.Scale());
                    r *= scale;
                    circles.Add(new Circle(new float2(pos.x, pos.z), r));
                    break;
                }
        }
        ParseNodeList(node.ChildNodes, transform);
    }


    float4x4 FromTransform(string transformText)
    {
        float3 translation = float3.zero;
        quaternion rotation = quaternion.identity;
        foreach(string part in transformText.Split(")"))
        {
            string name = part.Split("(")[0];
            switch(name)
            {
                case "translate":
                    {
                        // part == translate(-836.25, -161.25
                        string valueStr = part.Split("(")[1];
                        // value == -836.25, -161.25
                        string[] valueSeparate = valueStr.Split(",");
                        float val0 = float.Parse(valueSeparate[0]);
                        float val1 = valueSeparate.Length > 1 ? float.Parse(valueSeparate[1]) : 0;
                        translation = new float3(val0, 0, val1);
                        break;
                    }
                default: { break;  }
            }
        }

        return new float4x4(rotation, translation);
    }
}
