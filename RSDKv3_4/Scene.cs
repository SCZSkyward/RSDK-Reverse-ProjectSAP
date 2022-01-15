using System;
using System.Collections.Generic;
using System.Linq;
using RSDKv3_4;

namespace RSDKv3_4
{
    public abstract class Scene
    {
        public abstract class Entity
        {
            /// <summary>
            /// The type of object the entity is
            /// </summary>
            public byte type = 0;
            /// <summary>
            /// The entity's property value (aka subtype in classic sonic games)
            /// </summary>
            public byte propertyValue = 0;
            /// <summary>
            /// the x position (shifted 16-bit format, so 1.0 == (1 << 16))
            /// </summary>
            public int xpos = 0;
            /// <summary>
            /// the y position (shifted 16-bit format, so 1.0 == (1 << 16))
            /// </summary>
            public int ypos = 0;

            /// <summary>
            /// XPos but represented as a floating point number rather than a fixed point one
            /// </summary>
            public float xposF
            {
                get { return xpos / 65536.0f; }
                set { xpos = (int)(value / 65536); }
            }

            /// <summary>
            /// YPos but represented as a floating point number rather than a fixed point one
            /// </summary>
            public float yposF
            {
                get { return ypos / 65536.0f; }
                set { ypos = (int)(value / 65536); }
            }

            public abstract void read(Reader reader);

            public abstract void write(Writer writer);

        }

        public enum ActiveLayers
        {
            Foreground,
            Background1,
            Background2,
            Background3,
            Background4,
            Background5,
            Background6,
            Background7,
            Background8,
            None,
        }
        public enum LayerMidpoints
        {
            BeforeLayer0,
            AfterLayer0,
            AfterLayer1,
            AfterLayer2,
            AfterLayer3,
        }

        /// <summary>
        /// the stage's name (what the titlecard displays)
        /// </summary>
        public string title = "STAGE";

        /// <summary>
        /// the chunk layout for the FG layer
        /// </summary>
        public ushort[][] layout;

        /// <summary>
        /// Active Layer 0
        /// </summary>
        public ActiveLayers activeLayer0 = ActiveLayers.Background1;
        /// <summary>
        /// Active Layer 1
        /// </summary>
        public ActiveLayers activeLayer1 = ActiveLayers.None;
        /// <summary>
        /// Active Layer 2
        /// </summary>
        public ActiveLayers activeLayer2 = ActiveLayers.Foreground;
        /// <summary>
        /// Active Layer 3
        /// </summary>
        public ActiveLayers activeLayer3 = ActiveLayers.Foreground;
        /// <summary>
        /// Determines what layers should draw using high visual plane, in an example of 2, active layers 2 & 3 would use high plane tiles, while 0 & 1 would use low plane
        /// </summary>
        public LayerMidpoints layerMidpoint = LayerMidpoints.AfterLayer2;

        /// <summary>
        /// the list of entities in the stage
        /// </summary>
        public List<Entity> entities = new List<Entity>();

        /// <summary>
        /// stage width (in chunks)
        /// </summary>
        public byte width = 0;
        /// <summary>
        /// stage height (in chunks)
        /// </summary>
        public byte height = 0;

        /// <summary>
        /// the Max amount of entities that can be in a single stage
        /// </summary>
        public const int ENTITY_LIST_SIZE = 1024;

        public abstract void read(Reader reader);

        public void write(string filename)
        {
            using (Writer writer = new Writer(filename))
                write(writer);
        }

        public void write(System.IO.Stream stream)
        {
            using (Writer writer = new Writer(stream))
                write(writer);
        }

        public abstract void write(Writer writer);

        /// <summary>
        /// Resizes a layer.
        /// </summary>
        /// <param name="width">The new Width</param>
        /// <param name="height">The new Height</param>
        public void resize(byte width, byte height)
        {
            // first take a backup of the current dimensions
            // then update the internal dimensions
            byte oldWidth = this.width;
            byte oldHeight = this.height;
            this.width = width;
            this.height = height;

            // resize the tile map
            System.Array.Resize(ref layout, this.height);

            // fill the extended tile arrays with "empty" values

            // if we're actaully getting shorter, do nothing!
            for (byte i = oldHeight; i < this.height; i++)
            {
                // first create arrays child arrays to the old width
                // a little inefficient, but at least they'll all be equal sized
                layout[i] = new ushort[oldWidth];
                for (int j = 0; j < oldWidth; ++j)
                    layout[i][j] = 0; // fill the new ones with blanks
            }

            for (byte y = 0; y < this.height; y++)
            {
                // now resize all child arrays to the new width
                System.Array.Resize(ref layout[y], this.width);
                for (ushort x = oldWidth; x < this.width; x++)
                    layout[y][x] = 0; // and fill with blanks if wider
            }
        }
    }
}

namespace RSDKv3
{
    public class Scene : RSDKv3_4.Scene
    {
        public new class Entity : RSDKv3_4.Scene.Entity
        {
            public Entity() { }

            public Entity(byte type, byte propertyValue, int xpos, int ypos) : this()
            {
                this.type = type;
                this.propertyValue = propertyValue;
                this.xpos = xpos;
                this.ypos = ypos;
            }

            public Entity(Reader reader) : this()
            {
                read(reader);
            }

            public override void read(Reader reader)
            {
                // entity type, 1 byte, unsigned
                type = reader.ReadByte();
                // Property value, 1 byte, unsigned
                propertyValue = reader.ReadByte();

                // X Position, 2 bytes, big-endian, signed			
                xpos = (short)(reader.ReadSByte() << 8);
                xpos |= reader.ReadByte();
                xpos <<= 16;

                // Y Position, 2 bytes, big-endian, signed
                ypos = (short)(reader.ReadSByte() << 8);
                ypos |= reader.ReadByte();
                ypos <<= 16;
            }

            public override void write(Writer writer)
            {
                writer.Write(type);
                writer.Write(propertyValue);

                writer.Write((byte)(xpos >> 24));
                writer.Write((byte)((xpos >> 16) & 0xFF));

                writer.Write((byte)(ypos >> 24));
                writer.Write((byte)((ypos >> 16) & 0xFF));
            }
        }

        /// <summary>
        /// a list of names for each Object Type
        /// </summary>
        public List<string> objectTypeNames = new List<string>();

        public Scene()
        {
            layout = new ushort[1][];
            layout[0] = new ushort[1];
        }

        public Scene(string filename) : this(new Reader(filename)) { }

        public Scene(System.IO.Stream stream) : this(new Reader(stream)) { }

        public Scene(Reader reader)
        {
            read(reader);
        }

        public override void read(Reader reader)
        {
            title = reader.readRSDKString();

            activeLayer0 = (ActiveLayers)reader.ReadByte();
            activeLayer1 = (ActiveLayers)reader.ReadByte();
            activeLayer2 = (ActiveLayers)reader.ReadByte();
            activeLayer3 = (ActiveLayers)reader.ReadByte();
            layerMidpoint = (LayerMidpoints)reader.ReadByte();

            // Map width/height in 128 pixel units
            // In RSDKv3 it's one byte long

            width = reader.ReadByte();
            height = reader.ReadByte();

            layout = new ushort[height][];
            for (int i = 0; i < height; i++)
                layout[i] = new ushort[width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 128x128 Block number is 16-bit
                    // Big-Endian in RSDKv3
                    layout[y][x] = (ushort)(reader.ReadByte() << 8);
                    layout[y][x] |= reader.ReadByte();
                }
            }

            // Read number of object types
            int objectTypeCount = reader.ReadByte();

            objectTypeNames.Clear();
            for (int n = 0; n < objectTypeCount; n++)
                objectTypeNames.Add(reader.readRSDKString());

            // Read entities

            // 2 bytes, Big-Endian, unsigned
            int entityCount = reader.ReadByte() << 8;
            entityCount |= reader.ReadByte();

            for (int n = 0; n < entityCount; n++)
                entities.Add(new Entity(reader));

            reader.Close();
        }

        public override void write(Writer writer)
        {
            // Write zone name		
            writer.writeRSDKString(title);

            // Write the active layers & midpoint
            writer.Write((byte)activeLayer0);
            writer.Write((byte)activeLayer1);
            writer.Write((byte)activeLayer2);
            writer.Write((byte)activeLayer3);
            writer.Write((byte)layerMidpoint);

            // Write width and height
            writer.Write(width);
            writer.Write(height);

            // Write tile layout
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    writer.Write((byte)(layout[h][w] >> 8));
                    writer.Write((byte)(layout[h][w] & 0xff));
                }
            }

            // Write number of object types
            writer.Write((byte)objectTypeNames.Count);

            // Write object type names
            // Ignore first object type (Blank Object), it is not stored.
            foreach (string typeName in objectTypeNames)
                writer.writeRSDKString(typeName);

            // Write number of entities
            writer.Write((byte)(entities.Count >> 8));
            writer.Write((byte)(entities.Count & 0xFF));

            // Write entities
            foreach (Entity entity in entities)
                entity.write(writer);

            writer.Close();
        }
    }
}

namespace RSDKv4
{
    public class Scene : RSDKv3_4.Scene
    {
        public new class Entity : RSDKv3_4.Scene.Entity
        {
            public enum InkEffects { None, Blend, Alpha, Add, Sub }

            public enum Priorities { ActiveBounds, Active, ActivePaused, XBounds, XBoundsDestroy, Inactive, BoundsSmall, Unknown }

            public int? State;
            public Tiles128x128.Block.Tile.Directions? Direction;
            public int? Scale;
            public int? Rotation;
            public byte? DrawOrder;
            public Priorities? Priority;
            public byte? Alpha;
            public byte? Animation;
            public int? AnimationSpeed;
            public byte? Frame;
            public InkEffects? InkEffect;
            public int? Value0;
            public int? Value1;
            public int? Value2;
            public int? Value3;

            public Entity()
            {
            }

            public Entity(byte type, byte propertyValue, int xpos, int ypos)
            {
                this.type = type;
                this.propertyValue = propertyValue;
                this.xpos = xpos;
                this.ypos = ypos;
            }

            public Entity(Reader reader)
            {
                read(reader);
            }

            public override void read(Reader reader)
            {
                //Variable flags, 2 bytes, unsigned
                ushort flags = reader.ReadUInt16();

                // entity type, 1 byte, unsigned
                type = reader.ReadByte();

                // PropertyValue, 1 byte, unsigned
                propertyValue = reader.ReadByte();

                //a position, made of 8 bytes, 4 for X, 4 for Y
                xpos = reader.ReadInt32();
                ypos = reader.ReadInt32();

                if ((flags & 1) != 0)
                    State = reader.ReadInt32();
                else
                    State = null;
                if ((flags & 2) != 0)
                    Direction = (Tiles128x128.Block.Tile.Directions)reader.ReadByte();
                else
                    Direction = null;
                if ((flags & 4) != 0)
                    Scale = reader.ReadInt32();
                else
                    Scale = null;
                if ((flags & 8) != 0)
                    Rotation = reader.ReadInt32();
                else
                    Rotation = null;
                if ((flags & 16) != 0)
                    DrawOrder = reader.ReadByte();
                else
                    DrawOrder = null;
                if ((flags & 32) != 0)
                    Priority = (Priorities)reader.ReadByte();
                else
                    Priority = null;
                if ((flags & 64) != 0)
                    Alpha = reader.ReadByte();
                else
                    Alpha = null;
                if ((flags & 128) != 0)
                    Animation = reader.ReadByte();
                else
                    Animation = null;
                if ((flags & 256) != 0)
                    AnimationSpeed = reader.ReadInt32();
                else
                    AnimationSpeed = null;
                if ((flags & 512) != 0)
                    Frame = reader.ReadByte();
                else
                    Frame = null;
                if ((flags & 1024) != 0)
                    InkEffect = (InkEffects)reader.ReadByte();
                else
                    InkEffect = null;
                if ((flags & 2048) != 0)
                    Value0 = reader.ReadInt32();
                else
                    Value0 = null;
                if ((flags & 4096) != 0)
                    Value1 = reader.ReadInt32();
                else
                    Value1 = null;
                if ((flags & 8192) != 0)
                    Value2 = reader.ReadInt32();
                else
                    Value2 = null;
                if ((flags & 16384) != 0)
                    Value3 = reader.ReadInt32();
                else
                    Value3 = null;
            }

            public override void write(Writer writer)
            {
                int flags = 0;
                if (State.HasValue)
                    flags |= 1;
                if (Direction.HasValue)
                    flags |= 2;
                if (Scale.HasValue)
                    flags |= 4;
                if (Rotation.HasValue)
                    flags |= 8;
                if (DrawOrder.HasValue)
                    flags |= 16;
                if (Priority.HasValue)
                    flags |= 32;
                if (Alpha.HasValue)
                    flags |= 64;
                if (Animation.HasValue)
                    flags |= 128;
                if (AnimationSpeed.HasValue)
                    flags |= 256;
                if (Frame.HasValue)
                    flags |= 512;
                if (InkEffect.HasValue)
                    flags |= 1024;
                if (Value0.HasValue)
                    flags |= 2048;
                if (Value1.HasValue)
                    flags |= 4096;
                if (Value2.HasValue)
                    flags |= 8192;
                if (Value3.HasValue)
                    flags |= 16384;
                writer.Write((ushort)flags);

                writer.Write(type);
                writer.Write(propertyValue);

                writer.Write(xpos);
                writer.Write(ypos);

                if (State.HasValue)
                    writer.Write(State.Value);
                if (Direction.HasValue)
                    writer.Write((byte)Direction.Value);
                if (Scale.HasValue)
                    writer.Write(Scale.Value);
                if (Rotation.HasValue)
                    writer.Write(Rotation.Value);
                if (DrawOrder.HasValue)
                    writer.Write(DrawOrder.Value);
                if (Priority.HasValue)
                    writer.Write((byte)Priority.Value);
                if (Alpha.HasValue)
                    writer.Write(Alpha.Value);
                if (Animation.HasValue)
                    writer.Write(Animation.Value);
                if (AnimationSpeed.HasValue)
                    writer.Write(AnimationSpeed.Value);
                if (Frame.HasValue)
                    writer.Write(Frame.Value);
                if (InkEffect.HasValue)
                    writer.Write((byte)InkEffect.Value);
                if (Value0.HasValue)
                    writer.Write(Value0.Value);
                if (Value1.HasValue)
                    writer.Write(Value1.Value);
                if (Value2.HasValue)
                    writer.Write(Value2.Value);
                if (Value3.HasValue)
                    writer.Write(Value3.Value);
            }

        }

        public Scene()
        {
            layout = new ushort[1][];
            layout[0] = new ushort[1];
        }

        public Scene(string filename) : this(new Reader(filename)) { }

        public Scene(System.IO.Stream stream) : this(new Reader(stream)) { }

        public Scene(Reader reader)
        {
            read(reader);
        }

        public override void read(Reader reader)
        {
            title = reader.readRSDKString();

            activeLayer0  = (ActiveLayers)reader.ReadByte();
            activeLayer1  = (ActiveLayers)reader.ReadByte();
            activeLayer2  = (ActiveLayers)reader.ReadByte();
            activeLayer3  = (ActiveLayers)reader.ReadByte();
            layerMidpoint = (LayerMidpoints)reader.ReadByte();

            // Map width in 128 pixel units
            // In RSDKv4 it's one byte long (with an unused byte after each one), little-endian

            width = reader.ReadByte();
            reader.ReadByte(); // Unused

            height = reader.ReadByte();
            reader.ReadByte(); // Unused

            layout = new ushort[height][];
            for (int i = 0; i < height; i++)
                layout[i] = new ushort[width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 128x128 Block number is 16-bit
                    // Little-Endian in RSDKv4	
                    layout[y][x] = reader.ReadByte();
                    layout[y][x] |= (ushort)(reader.ReadByte() << 8);
                }
            }

            // Read entities

            // 2 bytes, little-endian, unsigned
            int entityCount = reader.ReadByte();
            entityCount |= reader.ReadByte() << 8;

            entities.Clear();
            for (int o = 0; o < entityCount; o++)
                entities.Add(new Entity(reader));

            reader.Close();
        }

        public override void write(Writer writer)
        {
            // Write zone name		
            writer.writeRSDKString(title);

            // Write the active layers & midpoint
            writer.Write((byte)activeLayer0);
            writer.Write((byte)activeLayer1);
            writer.Write((byte)activeLayer2);
            writer.Write((byte)activeLayer3);
            writer.Write((byte)layerMidpoint);

            // Write width
            writer.Write(width);
            writer.Write((byte)0);

            // Write height
            writer.Write(height);
            writer.Write((byte)0);

            // Write tile layout
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    writer.Write((byte)(layout[h][w] & 0xFF));
                    writer.Write((byte)(layout[h][w] >> 8));
                }
            }

            // Write number of entities

            writer.Write((byte)(entities.Count & 0xFF));
            writer.Write((byte)(entities.Count >> 8));

            // Write entities
            foreach (Entity entity in entities)
                entity.write(writer);

            writer.Close();

        }
    }
}
