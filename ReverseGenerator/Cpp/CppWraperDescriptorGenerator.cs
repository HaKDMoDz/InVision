﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InVision.Extensions;
using InVision.Native;

namespace ReverseGenerator.Cpp
{
    public class CppWraperDescriptorGenerator : CppValueObjectGeneratorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CppWraperDescriptorGenerator"/> class.
        /// </summary>
        /// <param name="configOptions">The options.</param>
        /// <param name="type">The type.</param>
        public CppWraperDescriptorGenerator(ConfigOptions configOptions, Type type)
            : base(configOptions, type)
        {
        }

        /// <summary>
        /// Generates this instance.
        /// </summary>
        /// <returns></returns>
        protected override void GenerateType(SourceWriter writer, string cppTypename)
        {
            writer.WriteLine("/**");
            writer.WriteLine(" * Type {0}", cppTypename);
            writer.WriteLine(" */");
            writer.WriteLine("struct {0}", cppTypename);
            writer.OpenBlock();

            WriteBaseDescriptorField(writer);
            WriteDescriptorFields(writer);

            writer.CloseBlock(";");
            writer.WriteLine();
        }

        /// <summary>
        /// Gets the typename.
        /// </summary>
        /// <returns></returns>
        protected override string GetTypename()
        {
            return ConfigOptions.GetCppWrapperDescriptorTypename(Type);
        }

        /// <summary>
        /// Writes the base descriptor field.
        /// </summary>
        /// <param name="writer">The writer.</param>
        private void WriteBaseDescriptorField(SourceWriter writer)
        {
            IEnumerable<Type> parentInterfaces =
                Type.GetInterfaces().Where(i => i.HasAttribute<CppClassAttribute>(true));
            var baseInterface =
                (from i in parentInterfaces
                 let attr = i.GetAttribute<CppClassAttribute>(true)
                 where attr.Type == ClassType.Concrete
                 select new { Interface = i, Attribute = attr }).SingleOrDefault();

            if (baseInterface != null)
            {
                const string fieldName = "base";
                string translatedType = ConfigOptions.GetCppWrapperDescriptorTypename(baseInterface.Interface);

                WriteField(writer, fieldName, translatedType);
            }
            else
            {
                WriteField(writer, "self", "InvHandle");
            }
        }

        /// <summary>
        /// Writes the descriptor fields.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="type">The type.</param>
        private void WriteDescriptorFields(SourceWriter writer)
        {
            IEnumerable<PropertyInfo> properties =
                from property in
                    Type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                where property.HasAttribute<FieldAttribute>(true)
                select property;

            foreach (PropertyInfo property in properties)
            {
                string fieldName = property.Name;
                string translatedType = ConfigOptions.ToCppTypename(property, property.PropertyType) + "*";

                WriteField(writer, fieldName, translatedType);
            }
        }
    }
}