using System;

public class PowerAttribute : Attribute
{
    public string RoleString { get; set; }

    public PowerAttribute(string roleString)
    {
        RoleString = roleString;
    }
}