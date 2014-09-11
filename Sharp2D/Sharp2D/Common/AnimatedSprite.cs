﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Reflection;
using Sharp2D.Core;
using Sharp2D.Core.Interfaces;
using Sharp2D.Game.Sprites;

namespace Sharp2D
{
    [Obsolete("AnimatedSprite has been moved to AnimationModule. Use a ModuleSprite and attach a AnimationModule!", true)]
    public abstract class AnimatedSprite : Sprite, ILogical
    {
        public Animation CurrentlyPlayingAnimation { get; internal set; }
        public Animation ChildAnimationPlaying { get; internal set; }

        private long LastTick;

        public AnimationHolder Animations { get; private set; }

        public AnimatedSprite Parent { get; private set; }

        public virtual string AnimationConfigPath
        {
            get
            {
                return "animations/" + Name + ".conf";
            }
        }

        public virtual string JsonResourcePath
        {
            get
            {
                return Name + ".conf";
            }
        }

        public override float X
        {
            get
            {
                return base.X;
            }
            set
            {
                float dif = value - base.X;
                
                base.X = value;

                foreach (AnimatedSprite child in children)
                {
                    //if (child is NullAnimatedSprite) //Empty animation?
                    //    continue;

                    child.X += dif;
                }
            }
        }

        public override float Y
        {
            get
            {
                return base.Y;
            }
            set
            {
                float dif = value - base.Y;

                base.Y = value;

                foreach (AnimatedSprite child in children)
                {
                    //if (child is NullAnimatedSprite) //Empty animation?
                    //    continue;

                    child.Y += dif;
                }
            }
        }

        private List<AnimatedSprite> children = new List<AnimatedSprite>();

        public virtual void Update()
        {
            if (CurrentlyPlayingAnimation != null)
            {
                if (LastTick == 0)
                    LastTick = Screen.TickCount;

                if (LastTick + CurrentlyPlayingAnimation.Speed <= Screen.TickCount)
                {
                    LastTick = Screen.TickCount;
                    if (CurrentlyPlayingAnimation.Playing)
                    {
                        if (!CurrentlyPlayingAnimation.Reverse)
                            CurrentlyPlayingAnimation.CurrentStep++;
                        else
                            CurrentlyPlayingAnimation.CurrentStep--;
                    }
                }
                TexCoords = CurrentlyPlayingAnimation.CurrentTexCoords;
            }
        }

        public void ClearAnimations()
        {
            Animations = null;
            foreach (AnimatedSprite ani in children)
            {
                ani.ClearAnimations();
            }
            children.Clear();
        }

        protected override void OnLoad()
        {
            if (Parent == null && Animations == null) //Only load json if we have no parent and if we haven't already loaded the json
                LoadJSON();
        }

        private void LoadJSON()
        {
            string json = null;
            if (File.Exists(AnimationConfigPath))
            {
                json = File.ReadAllText(AnimationConfigPath);
            }
            else
            {
                Assembly asm = Assembly.GetEntryAssembly();

                Stream stream = asm.GetManifestResourceStream(JsonResourcePath);
                if (stream == null)
                    return;
                StreamReader reader = new StreamReader(stream);
                json = reader.ReadToEnd();

                reader.Close();
                stream.Close();

                reader.Dispose();
                stream.Dispose();
            }

            if (json != null)
            {

                Animations = JsonConvert.DeserializeObject<AnimationHolder>(json);
                //Animations[0].Owner = this;
                Width = Animations[0].Width;
                Height = Animations[0].Height;

                children = SetupChildren(Animations);

                Animations[0].Playing = true;
            }
        }

        protected override void OnDispose()
        {
            if (Animations != null)
                Animations.Dispose();
        }

        private List<AnimatedSprite> SetupChildren(AnimationHolder animations)
        {
            List<AnimatedSprite> temp = new List<AnimatedSprite>();
            foreach (string key in animations.Animations.Keys) //To follow this, let's assume this is the "walking left" animation
            {
                Animation ani = animations.Animations[key];

                temp.AddRange(Setup(ref ani));
            }

            return temp; //Return the new sprites created
        }

        private List<AnimatedSprite> Setup(ref Animation ani, List<Animation> inheritStack = null)
        {
            List<AnimatedSprite> temp = new List<AnimatedSprite>();

            if (ani.setup_ran)
                return temp;

            if (inheritStack == null)
                inheritStack = new List<Animation>();

            Assembly assembly = Assembly.GetEntryAssembly();

            //ani.Owner = this;

            if (ani.Animations == null)
                return temp;

            if (ani.InheritedAnimations != null)
            {
                if (inheritStack.Count > 0 && inheritStack.Contains(ani.InheritedAnimationOwner))
                    throw new InvalidOperationException("Loop inheritance detected!");
                if (!ani.InheritedAnimationOwner.setup_ran) //If the inherited animation hasn't been setup yet
                {
                    Animation inherit_owner = ani.InheritedAnimationOwner;
                    inheritStack.Add(ani); //Add ourself to the stack
                    Setup(ref inherit_owner, inheritStack); //Setup the inherited animation
                    inheritStack.Remove(ani); //Remove ourself from the stack
                }
                AnimationHolder t = ani.Animations;
                AnimationHolder.Combind(ani.InheritedAnimations, ref t);
            }

            foreach (string ckey in ani.Animations.Animations.Keys) //To follow this, let's assume this is the "hat" animation for "walking left" animation
            {
                //ckey = "hat"
                Animation child_animation = ani.Animations[ckey]; //Same as ani.Animations.Animations[ckey];
                //Get the animation object for "hat"

                child_animation.ParentAnimation = ani; //Set the parent of this child animation to this

                if (ani.InheritedAnimations == null) //Only inherit values if this animation didn't inherit animations from another animation
                {
                    Animation.Combind(ani, ref child_animation, false); //Inherit values from parent animation to this animation (exclude animations)
                }

                if (!child_animation.IsEmpty)
                {
                    /*AnimatedSprite sprite;
                    Type st = assembly.GetType(child_animation.SpriteFullName); //Get the type for the FullSpriteName
                    if (st == null)
                    {
                        //Assume it's a file path if no type was found
                        sprite = new BasicAnimatedSprite(child_animation.SpriteFullName);
                    }
                    else
                    {
                        sprite = (AnimatedSprite)Activator.CreateInstance(st); //Create a new instance of the child sprite
                    }

                    child_animation.Owner = sprite; //Set the owner of the "hat" animation to the newly created sprite
                    sprite.Animations = child_animation.Animations; //Set the animations for the hat sprite to the children animations of the "hat" animation.
                    sprite.Parent = this; //Set the parent of the hat sprite to this
                    sprite.IsVisible = false; //Make sure this sprite isn't visible by default
                    temp.Add(sprite); //Add the "hat" sprite as a children of this sprite

                    sprite.SetupChildren(sprite.Animations); //Setup the children of "hat" animation, and put all of it's children in a list

                    //Nevermind, don't do that
                    //temp.AddRange(temp2); //Add all of children of the "hat" sprite as our children as well.*/
                }
                else
                {
                    //temp.Add(new NullAnimatedSprite()); //This is an empty animation, fill it with a null sprite
                }
            }

            ani.setup_ran = true;

            return temp;
        }

        public void AlignChildAnimation()
        {
            /*if (ChildAnimationPlaying == null)
                return;
            AnimatedSprite sprite = ChildAnimationPlaying.Owner;

            //Default is center parent origin, center child origin
            float defaultx = X + (Width / 2f) - (ChildAnimationPlaying.Width / 2f);
            float defaulty = Y - (Height / 2f) + (ChildAnimationPlaying.Height / 2f);

            Placement porigin = ChildAnimationPlaying.ParentOriginType;
            if ((porigin & Placement.Left) != 0)
            {
                defaultx -= (Width / 2f);
            }
            if ((porigin & Placement.Right) != 0)
            {
                defaultx += Width / 2f;
            }
            if ((porigin & Placement.Top) != 0)
            {
                defaulty -= Height / 2f;
            }
            if ((porigin & Placement.Bottom) != 0)
            {
                defaulty += (Height / 2f);
            }

            float xadd = 0f, yadd = 0f;
            Placement origin = ChildAnimationPlaying.OriginType;
            if ((origin & Placement.Left) != 0)
            {
                xadd = -(ChildAnimationPlaying.Width / 2f);
            }
            if ((origin & Placement.Right) != 0)
            {
                xadd = ChildAnimationPlaying.Width / 2f;
            }
            if ((origin & Placement.Top) != 0)
            {
                yadd = ChildAnimationPlaying.Height / 2f;
            }
            if ((origin & Placement.Bottom) != 0)
            {
                yadd = -(ChildAnimationPlaying.Height / 2f);
            }

            //Move the "hat" sprite to the default origin of the sprite
            sprite.X = defaultx - xadd;
            sprite.Y = defaulty + yadd;

            //Offset the "hat" sprite
            sprite.X += ChildAnimationPlaying.XOffset;
            sprite.Y += ChildAnimationPlaying.YOffset;

            sprite.AlignChildAnimation();*/
        }
    }
}