﻿using System;
using System.Collections.Generic;
using System.IO;

namespace ReverseGenerator.CSharp
{
    public abstract class CSharpGeneratorBase
    {
        private readonly ConfigOptions _configOptions;
        private SourceWriter _writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpGeneratorBase"/> class.
        /// </summary>
        /// <param name="configOptions">The config options.</param>
        protected CSharpGeneratorBase(ConfigOptions configOptions)
        {
            _configOptions = configOptions;
        }

        /// <summary>
        /// Gets the config options.
        /// </summary>
        /// <value>The config options.</value>
        public ConfigOptions ConfigOptions
        {
            get { return _configOptions; }
        }

        /// <summary>
        /// Gets the writer.
        /// </summary>
        /// <value>The writer.</value>
        protected SourceWriter Writer
        {
            get { return _writer; }
        }

        /// <summary>
        /// Generates the specified types.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <param name="suffix">The suffix.</param>
        public void Generate(IEnumerable<Type> types, string suffix)
        {
            var outputFile = _configOptions.ProjectName + suffix + ".cs";
            outputFile = outputFile.Replace("_", "");
            outputFile = Path.Combine(_configOptions.CsOutputDir, outputFile);

            using (_writer = new SourceWriter(outputFile))
            {
                _writer.WriteLine("/*");
                _writer.WriteLine(" * GENERATED CODE");
                _writer.WriteLine(" * DO NOT EDIT THIS");
                _writer.WriteLine(" */");
                _writer.WriteLine();

                GenerateContent(types);
            }
        }

        /// <summary>
        /// Generates the content.
        /// </summary>
        /// <param name="types">The types.</param>
        protected abstract void GenerateContent(IEnumerable<Type> types);
    }
}