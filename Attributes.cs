﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Datamodel
{
    /// <summary>
    /// A name/value pair associated with an <see cref="Element"/>.
    /// </summary>
    public class Attribute : INotifyPropertyChanged
    {
        internal Attribute(Element owner, string name, object value, long offset)
        {
            Name = name;
            Value = value;
            Offset = offset;
            Owner = owner;
            if (Owner.Attributes.Count == Int32.MaxValue)
                throw new InvalidOperationException("Maximum Attribute count reached for this Element.");
            Owner.Attributes.Add(this);
        }

        #region Properties
        /// <summary>
        /// The name of this Attribute.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; NotifyPropertyChanged("Name"); }
        }
        string name;

        /// <summary>
        /// The value held by this Attribute.
        /// </summary>
        public object Value
        {
            get
            {
                // read deferred
                if (Offset > 0)
                {
                    try
                    {
                        lock (Owner.Owner.Codec)
                        {
                            value = Owner.Owner.Codec.DeferredDecodeAttribute(Owner.Owner, Offset);
                        }
                    }
                    catch (Exception err)
                    {
                        throw new CodecException(String.Format("Deferred loading of attribute \"{0}\" on element {1} using codec {2} threw an exception.", Name, Owner.ID, Owner.Owner.Codec), err);
                    }
                    Offset = 0;
                }

                // expand stubs
                if (Owner.Owner.ElementsAdded > LastStubSearch)
                {
                    lock (Owner.Owner.AllElements.ChangeLock)
                    {
                        var elem = value as Element;
                        if (elem != null)
                        {
                            if (elem.Stub) value = Owner.Owner.AllElements[elem.ID] ?? value;
                        }
                        else
                        {
                            var elem_list = value as List<Element>;
                            if (elem_list != null && elem_list.Any(e => e != null && e.Stub)) // threading this query is slower!
                                value = elem_list.AsParallel().Select(e => e.Stub ? Owner.Owner.AllElements[e.ID] ?? e : e).ToList();
                        }
                    }
                }
                LastStubSearch = Owner.Owner.ElementsAdded;

                return value;
            }
            set
            {
                this.value = value;
                Offset = 0;
                NotifyPropertyChanged("Value");
            }
        }
        object value;
        #endregion

        long Offset;
        Element Owner;
        int LastStubSearch = 0;

        public override string ToString()
        {
            var type = Value.GetType();
            return String.Format("{0} <{1}>", Name, type.IsGenericType ? type.GetGenericArguments()[0].FullName + "[]" : type.FullName);
        }

        #region Interfaces
        /// <summary>
        /// Raised when the <see cref="Name"/> or <see cref="Value"/> of the Attribute changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }
        #endregion
    }

    public abstract class VectorBase : IEnumerable<float>, INotifyPropertyChanged
    {

        /// <summary>
        /// Raised when a value in the enumerable changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        public abstract IEnumerator<float> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public override string ToString()
        {
            return String.Join(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator, this.ToArray());
        }

        public static bool operator ==(VectorBase x, VectorBase y)
        {
            if (x.GetType() != y.GetType()) return false;

            var ex = x.GetEnumerator();
            var ey = y.GetEnumerator();
            while (ex.MoveNext())
            {
                ey.MoveNext();
                if (ex.Current != ey.Current) return false;
            }
            return true;
        }

        public static bool operator !=(VectorBase x, VectorBase y)
        {
            return !(x == y);
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == this.GetType() ? obj as VectorBase == this as VectorBase : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var ord in this)
                hash ^= ord.GetHashCode();
            return hash;
        }
    }

    public class Vector2 : VectorBase
    {
        public float X { get { return x; } set { x = value; NotifyPropertyChanged("X"); } }
        public float Y { get { return y; } set { y = value; NotifyPropertyChanged("Y"); } }

        float x;
        float y;

        public Vector2()
        { }

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public Vector2(IEnumerable<float> values)
        {
            int i = 0;
            foreach (var ordinate in values.Take(2))
            {
                switch (i)
                {
                    case 0: X = ordinate; break;
                    case 1: Y = ordinate; break;
                }
                i++;
            }
        }

        public override IEnumerator<float> GetEnumerator()
        {
            return new float[] { X, Y }.ToList().GetEnumerator();
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2 operator *(Vector2 a, float b)
        {
            return new Vector2(a.X * b, a.Y * b);
        }

        public static Vector2 operator /(Vector2 a, float b)
        {
            return new Vector2(a.X / b, a.Y / b);
        }
    }

    public class Vector3 : Vector2
    {
        public float Z { get { return z; } set { z = value; NotifyPropertyChanged("Z"); } }
        float z;

        public Vector3()
        { }

        public Vector3(float x, float y, float z)
            : base(x, y)
        {
            this.z = z;
        }
        public Vector3(IEnumerable<float> values)
        {
            int i = 0;
            foreach (var ordinate in values.Take(3))
            {
                switch (i)
                {
                    case 0: X = ordinate; break;
                    case 1: Y = ordinate; break;
                    case 2: Z = ordinate; break;
                }
                i++;
            }
        }

        public override IEnumerator<float> GetEnumerator()
        {
            return new float[] { X, Y, Z }.ToList().GetEnumerator();
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator *(Vector3 a, float b)
        {
            return new Vector3(a.X * b, a.Y * b, a.Z * b);
        }

        public static Vector3 operator /(Vector3 a, float b)
        {
            return new Vector3(a.X / b, a.Y / b, a.Z / b);
        }
    }

    public class Angle : Vector3
    {
        public Angle()
            : base()
        { }

        public Angle(float x, float y, float z)
            : base(x, y, z)
        { }
        public Angle(IEnumerable<float> values)
            : base(values)
        { }

        public static Angle operator -(Angle a, Angle b)
        {
            return new Angle(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Angle operator +(Angle a, Angle b)
        {
            return new Angle(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Angle operator *(Angle a, float b)
        {
            return new Angle(a.X * b, a.Y * b, a.Z * b);
        }

        public static Angle operator /(Angle a, float b)
        {
            return new Angle(a.X / b, a.Y / b, a.Z / b);
        }
    }

    public class Vector4 : Vector3
    {
        public float W { get { return w; } set { w = value; NotifyPropertyChanged("W"); } }
        float w;

        public Vector4()
        {
        }

        public Vector4(float x, float y, float z, float w)
            : base(x, y, z)
        {
            this.w = w;
        }
        public Vector4(IEnumerable<float> values)
        {
            int i = 0;
            foreach (var ordinate in values.Take(4))
            {
                switch (i)
                {
                    case 0: X = ordinate; break;
                    case 1: Y = ordinate; break;
                    case 2: Z = ordinate; break;
                    case 3: W = ordinate; break;
                }
                i++;
            }
        }

        public override IEnumerator<float> GetEnumerator()
        {
            return new float[] { X, Y, Z, W }.ToList().GetEnumerator();
        }

        public static Vector4 operator -(Vector4 a, Vector4 b)
        {
            return new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }

        public static Vector4 operator +(Vector4 a, Vector4 b)
        {
            return new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        public static Vector4 operator *(Vector4 a, float b)
        {
            return new Vector4(a.X * b, a.Y * b, a.Z * b, a.W * b);
        }

        public static Vector4 operator /(Vector4 a, float b)
        {
            return new Vector4(a.X / b, a.Y / b, a.Z / b, a.W / b);
        }
    }

    public class Quaternion : Vector4
    {
        public Quaternion()
            : base()
        { }

        public Quaternion(float x, float y, float z, float w)
            : base(x, y, z, w)
        { }
        public Quaternion(IEnumerable<float> values)
            : base(values)
        { }
    }

    public class Matrix : VectorBase
    {
        public Vector4 Column0 { get { return column0; } set { column0 = value; NotifyPropertyChanged("Row0"); } }
        public Vector4 Column1 { get { return column1; } set { column1 = value; NotifyPropertyChanged("Row1"); } }
        public Vector4 Column2 { get { return column2; } set { column2 = value; NotifyPropertyChanged("Row2"); } }
        public Vector4 Column3 { get { return column3; } set { column3 = value; NotifyPropertyChanged("Row3"); } }

        Vector4 column0 = new Vector4();
        Vector4 column1 = new Vector4();
        Vector4 column2 = new Vector4();
        Vector4 column3 = new Vector4();

        public Matrix()
        { }

        public Matrix(float[,] value)
        {
            if (value.GetUpperBound(0) < 4)
                throw new InvalidOperationException("Not enough columns for a Matrix4.");

            column0 = new Vector4(value.GetValue(0) as float[]);
            column1 = new Vector4(value.GetValue(1) as float[]);
            column2 = new Vector4(value.GetValue(2) as float[]);
            column3 = new Vector4(value.GetValue(3) as float[]);
        }

        public Matrix(IEnumerable<float> values)
        {
            if (values.Count() < 4 * 4)
                throw new ArgumentException("Not enough values for a Matrix4.");

            column0 = new Vector4(values.Take(4));
            column1 = new Vector4(values.Skip(4).Take(4));
            column2 = new Vector4(values.Skip(8).Take(4));
            column3 = new Vector4(values.Skip(12).Take(4));
        }

        public override string ToString()
        {
            return String.Join("\n", Column0, Column1, Column2, Column3);
        }

        public override IEnumerator<float> GetEnumerator()
        {
            return Column0.Concat(Column1.Concat(Column2.Concat(Column3))).GetEnumerator();
        }
    }
}