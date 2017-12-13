﻿// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RGB.NET.Core.Layout;

namespace RGB.NET.Core
{
    /// <inheritdoc cref="AbstractBindable" />
    /// <inheritdoc cref="IRGBDevice{TDeviceInfo}" />
    /// <summary>
    /// Represents a generic RGB-device
    /// </summary>
    public abstract class AbstractRGBDevice<TDeviceInfo> : AbstractBindable, IRGBDevice<TDeviceInfo>
        where TDeviceInfo : IRGBDeviceInfo
    {
        #region Properties & Fields

        /// <inheritdoc />
        public abstract TDeviceInfo DeviceInfo { get; }

        /// <inheritdoc />
        IRGBDeviceInfo IRGBDevice.DeviceInfo => DeviceInfo;

        private Size _size = Size.Invalid;
        /// <inheritdoc />
        public Size Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        private Point _location = new Point(0, 0);
        /// <inheritdoc />
        public Point Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        /// <inheritdoc />
        public DeviceUpdateMode UpdateMode { get; set; } = DeviceUpdateMode.Sync;

        /// <summary>
        /// Gets a dictionary containing all <see cref="Led"/> of the <see cref="IRGBDevice"/>.
        /// </summary>
        protected Dictionary<LedId, Led> LedMapping { get; } = new Dictionary<LedId, Led>();

        /// <summary>
        /// Gets a dictionary containing all <see cref="IRGBDeviceSpecialPart"/> associated with this <see cref="IRGBDevice"/>.
        /// </summary>
        protected Dictionary<Type, IRGBDeviceSpecialPart> SpecialDeviceParts { get; } = new Dictionary<Type, IRGBDeviceSpecialPart>();

        #region Indexer

        /// <inheritdoc />
        Led IRGBDevice.this[LedId ledId] => LedMapping.TryGetValue(ledId, out Led led) ? led : null;

        /// <inheritdoc />
        Led IRGBDevice.this[Point location] => LedMapping.Values.FirstOrDefault(x => x.LedRectangle.Contains(location));

        /// <inheritdoc />
        IEnumerable<Led> IRGBDevice.this[Rectangle referenceRect, double minOverlayPercentage]
            => LedMapping.Values.Where(x => referenceRect.CalculateIntersectPercentage(x.LedRectangle) >= minOverlayPercentage);

        #endregion

        #endregion

        #region Methods

        /// <inheritdoc />
        public virtual void Update(bool render = true, bool flushLeds = false)
        {
            // Device-specific updates
            DeviceUpdate();

            // Send LEDs to SDK
            IEnumerable<Led> ledsToUpdate = (flushLeds ? LedMapping.Values : LedMapping.Values.Where(x => x.IsDirty)).ToList();
            foreach (Led ledToUpdate in ledsToUpdate)
                ledToUpdate.Update();

            if (UpdateMode.HasFlag(DeviceUpdateMode.Sync))
                UpdateLeds(ledsToUpdate);
        }

        /// <inheritdoc />
        public virtual void SyncBack()
        { }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            SpecialDeviceParts.Clear();
            LedMapping.Clear();
        }

        /// <summary>
        /// Performs device specific updates.
        /// </summary>
        protected virtual void DeviceUpdate()
        { }

        /// <summary>
        /// Sends all the updated <see cref="Led"/> to the device.
        /// </summary>
        protected abstract void UpdateLeds(IEnumerable<Led> ledsToUpdate);

        /// <summary>
        /// Initializes the <see cref="Led"/> with the specified id.
        /// </summary>
        /// <param name="ledId">The <see cref="LedId"/> to initialize.</param>
        /// <param name="ledRectangle">The <see cref="Rectangle"/> representing the position of the <see cref="Led"/> to initialize.</param>
        /// <returns></returns>
        protected virtual Led InitializeLed(LedId ledId, Rectangle ledRectangle)
        {
            if ((ledId == LedId.Invalid) || LedMapping.ContainsKey(ledId)) return null;

            Led led = new Led(this, ledId, ledRectangle, CreateLedCustomData(ledId));
            LedMapping.Add(ledId, led);
            return led;
        }

        /// <summary>
        /// Applies the given layout.
        /// </summary>
        /// <param name="layoutPath">The file containing the layout.</param>
        /// <param name="imageLayout">The name of the layout used to get the images of the leds.</param>
        /// <param name="imageBasePath">The path images for this device are collected in.</param>
        /// <param name="createMissingLeds">If set to true a new led is initialized for every id in the layout if it doesn't already exist.</param>
        protected virtual void ApplyLayoutFromFile(string layoutPath, string imageLayout, string imageBasePath, bool createMissingLeds = false)
        {
            DeviceLayout layout = DeviceLayout.Load(layoutPath);
            if (layout != null)
            {
                LedImageLayout ledImageLayout = layout.LedImageLayouts.FirstOrDefault(x => string.Equals(x.Layout, imageLayout, StringComparison.OrdinalIgnoreCase));

                Size = new Size(layout.Width, layout.Height);

                if (layout.Leds != null)
                    foreach (LedLayout layoutLed in layout.Leds)
                    {
                        if (Enum.TryParse(layoutLed.Id, true, out LedId ledId))
                        {
                            if (!LedMapping.TryGetValue(ledId, out Led led) && createMissingLeds)
                                led = InitializeLed(ledId, new Rectangle());

                            if (led != null)
                            {
                                led.LedRectangle.Location = new Point(layoutLed.X, layoutLed.Y);
                                led.LedRectangle.Size = new Size(layoutLed.Width, layoutLed.Height);

                                led.Shape = layoutLed.Shape;
                                led.ShapeData = layoutLed.ShapeData;

                                LedImage image = ledImageLayout?.LedImages.FirstOrDefault(x => x.Id == layoutLed.Id);
                                led.Image = (!string.IsNullOrEmpty(image?.Image))
                                    ? new Uri(Path.Combine(imageBasePath, image.Image), UriKind.Absolute)
                                    : new Uri(Path.Combine(imageBasePath, "Missing.png"), UriKind.Absolute);
                            }
                        }
                    }
            }
        }

        /// <summary>
        /// Creates provider-specific data associated with this <see cref="LedId"/>.
        /// </summary>
        /// <param name="ledId">The <see cref="LedId"/>.</param>
        protected virtual object CreateLedCustomData(LedId ledId) => null;

        /// <inheritdoc />
        public void AddSpecialDevicePart<T>(T specialDevicePart)
            where T : class, IRGBDeviceSpecialPart
            => SpecialDeviceParts[typeof(T)] = specialDevicePart;

        /// <inheritdoc />
        public T GetSpecialDevicePart<T>()
            where T : class, IRGBDeviceSpecialPart
            => SpecialDeviceParts.TryGetValue(typeof(T), out IRGBDeviceSpecialPart devicePart) ? (T)devicePart : default(T);

        #region Enumerator

        /// <inheritdoc />
        /// <summary>
        /// Returns an enumerator that iterates over all <see cref="T:RGB.NET.Core.Led" /> of the <see cref="T:RGB.NET.Core.IRGBDevice" />.
        /// </summary>
        /// <returns>An enumerator for all <see cref="T:RGB.NET.Core.Led" /> of the <see cref="T:RGB.NET.Core.IRGBDevice" />.</returns>
        public IEnumerator<Led> GetEnumerator() => LedMapping.Values.GetEnumerator();

        /// <inheritdoc />
        /// <summary>
        /// Returns an enumerator that iterates over all <see cref="T:RGB.NET.Core.Led" /> of the <see cref="T:RGB.NET.Core.IRGBDevice" />.
        /// </summary>
        /// <returns>An enumerator for all <see cref="T:RGB.NET.Core.Led" /> of the <see cref="T:RGB.NET.Core.IRGBDevice" />.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}
