using System;

namespace FlutterWebDeploy.Models
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FlutterArgumentAttribute : Attribute
    {
        private readonly string argument;

        public FlutterArgumentAttribute(string argument)
        {
            this.argument = argument;
        }
    }
}