﻿using InVision.OIS.Native;

namespace InVision.OIS.Devices
{
    public unsafe class Axis : Component
    {
        private int* _abs;
        private int* _rel;
        private bool* _absOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="Axis"/> class.
        /// </summary>
        /// <param name="cppInstance">The CPP instance.</param>
        protected Axis(IComponent cppInstance)
            : base(cppInstance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Axis"/> class.
        /// </summary>
        public Axis()
            : this(CreateCppInstance<IAxis>())
        {
            var descriptor = new AxisDescriptor();
            Native.Construct(ref descriptor);

            Initialize(descriptor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Axis"/> class.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        internal Axis(AxisDescriptor descriptor)
            : this(CreateCppInstance<IAxis>())
        {
            Native.Self = descriptor.Base.Self;

            Initialize(descriptor);
        }

        /// <summary>
        /// Initializes the specified descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        protected void Initialize(AxisDescriptor descriptor)
        {
            Initialize(descriptor.Base);

            _abs = descriptor.Abs;
            _rel = descriptor.Rel;
            _absOnly = descriptor.AbsOnly;
        }

        /// <summary>
        /// Gets the native.
        /// </summary>
        /// <value>The native.</value>
        public new IAxis Native
        {
            get { return (IAxis)base.Native; }
        }

        /// <summary>
        /// Gets the absolute.
        /// </summary>
        /// <value>The absolute.</value>
        public int Absolute
        {
            get { return *_abs; }
        }

        /// <summary>
        /// Gets the relative.
        /// </summary>
        /// <value>The relative.</value>
        public int Relative
        {
            get { return *_rel; }
        }

        /// <summary>
        /// Gets a value indicating whether [absolute only].
        /// </summary>
        /// <value><c>true</c> if [absolute only]; otherwise, <c>false</c>.</value>
        public bool AbsoluteOnly
        {
            get { return *_absOnly; }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _abs = null;
                _rel = null;
                _absOnly = null;
            }
        }
    }
}