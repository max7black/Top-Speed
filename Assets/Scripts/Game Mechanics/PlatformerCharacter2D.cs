using System;
using UnityEngine;

namespace UnityStandardAssets._2D
{
    public class PlatformerCharacter2D : MonoBehaviour
    {
        [SerializeField] private float m_MaxSpeed = 10f;                    // The fastest the player can travel in the x axis.
        [SerializeField] private float m_JumpForce = 400f;                  // Amount of force added when the player jumps.
        [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;  // Amount of maxSpeed applied to crouching movement. 1 = 100%
        [SerializeField] private bool m_AirControl = false;                 // Whether or not a player can steer while jumping;
        [SerializeField] private LayerMask m_WhatIsGround;                  // A mask determining what is ground to the character
        [SerializeField] private float m_DefaultSpeed = 5f;                  // default speed is how fast the player goes on a flat surface
        

        private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
        const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
        private bool m_Grounded;            // Whether or not the player is grounded.
        private Transform m_CeilingCheck;   // A position marking where to check for ceilings
        const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up
        private Animator m_Anim;            // Reference to the player's animator component.
        private Rigidbody2D m_Rigidbody2D;
        private bool m_FacingRight = true;  // For determining which way the player is currently facing.
        private float angle;                // angle of player relative to the ground they are standing on
        private float slopedir;
        private Transform TwoDCharacter;
        private float m_Speed = 0.0f;       // current speed of player
        private float m_MinSpeed = 0.0f;           // the minimum speed the player can go determined while trying to run

        private float dampingRate = 0.1f;
        // This determines how far it can detect the ground from
        private float rayLength = 1.0f;
        // Set this up with the correct 'ground' layers
        private LayerMask maskGround = 1 << 8;
        Vector3 currentUp = Vector3.up;
        public Quaternion rotation;
        public Vector3 zRotation;
        public static float currentVelocity = 0.0f;
        

        private void Awake()
        {
            // Setting up references.
            m_GroundCheck = transform.Find("GroundCheck");
            m_CeilingCheck = transform.Find("CeilingCheck");
            m_Anim = GetComponent<Animator>();
            m_Rigidbody2D = GetComponent<Rigidbody2D>();

        }


        private void FixedUpdate()
        {
            m_Grounded = false;

            // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
            // This can be done using layers instead but Sample Assets will not overwrite your project settings.
            Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != gameObject)
                    m_Grounded = true;
            }
            m_Anim.SetBool("Ground", m_Grounded);

            // Set the vertical animation
            m_Anim.SetFloat("vSpeed", m_Rigidbody2D.velocity.y);

            // Casts a ray and hits if it finds the layermask ground
            RaycastHit2D[] hit = new RaycastHit2D[1];   // we want only the first hit
            if (Physics2D.RaycastNonAlloc(transform.position, -currentUp, hit, rayLength, maskGround) == 1) //enters if statment if the raycast hits something with layer mask "Ground"
            {
                currentUp = hit[0].normal;      // finds the normal vector of the ray. This lets up know what direction is perpendicular with grounds slope
            }

            // Sets the rotation based on the currentUp then scales it to be within the -90 to 90 and inverts it, other wise player rotates wrong direction down/up slopes
            m_Rigidbody2D.rotation = (Mathf.Atan2(currentUp.x, currentUp.y) * -100) / 2;
            
            Speed();
            currentVelocity = m_Rigidbody2D.velocity.x;
        }
    

        public void Move(float move, bool crouch, bool jump)
        {
            // If crouching, check to see if the character can stand up
            if (!crouch && m_Anim.GetBool("Crouch"))
            {
                // If the character has a ceiling preventing them from standing up, keep them crouching
                if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
                {
                    crouch = true;
                }
            }

            // Set whether or not the character is crouching in the animator
            m_Anim.SetBool("Crouch", crouch);

            //only control the player if grounded or airControl is turned on
            if (m_Grounded || m_AirControl)
            {
                // Reduce the speed if crouching by the crouchSpeed multiplier
                move = (crouch ? move*m_CrouchSpeed : move);

                // The Speed animator parameter is set to the absolute value of the horizontal input.
                m_Anim.SetFloat("Speed", Mathf.Abs(move));

                // Move the character
                if (m_Rigidbody2D.velocity.x == 0f)
                {
                    m_Rigidbody2D.velocity = new Vector2(move * m_DefaultSpeed, m_Rigidbody2D.velocity.y);
                    m_Speed = m_DefaultSpeed;
                }
                else
                {
                    m_Rigidbody2D.velocity = new Vector2(move * m_Speed, m_Rigidbody2D.velocity.y);
                }

                // If the input is moving the player right and the player is facing left...
                if (move > 0 && !m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
                    // Otherwise if the input is moving the player left and the player is facing right...
                else if (move < 0 && m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
            }
            // If the player should jump...
            if (m_Grounded && jump && m_Anim.GetBool("Ground"))
            {
                // Add a vertical force to the player.
                m_Grounded = false;
                m_Anim.SetBool("Ground", false);
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
            }
        }


        private void Flip()
        {
            // Switch the way the player is labelled as facing.
            m_FacingRight = !m_FacingRight;

            // Multiply the player's x local scale by -1.
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }

        private void Speed()
        {
            // Find minimum speed based on the angle of the slope the player is running on. If it's less than a 60 degree angle then min speed should be 
            // that 10 minus the angle*0.1, so between 10 and 4. If the angle is greater than 60 then the players speed will eventually be zero.
            if ((m_Rigidbody2D.velocity.x >= 0 && m_Rigidbody2D.velocity.x <= 60) || (m_Rigidbody2D.velocity.x <= 0 && m_Rigidbody2D.velocity.x >= -60) ||
                (m_Rigidbody2D.velocity.x >= 360 && m_Rigidbody2D.velocity.x <= 300) || (m_Rigidbody2D.velocity.x <= -360 && m_Rigidbody2D.velocity.x >= -300))
            {
                m_MinSpeed = 10.0f - (m_Rigidbody2D.rotation * 0.1f);
            }
            else
            {
                m_MinSpeed = 0.0f;
            }
            
            // if player if facing right and his rotation between 0->90 or -360->-270 then he's going up hill. Thus, his velocity should slowly decrease over time.
            if (((m_Rigidbody2D.rotation > 0 && m_Rigidbody2D.rotation <= 90) || (m_Rigidbody2D.rotation > -360 && m_Rigidbody2D.rotation <= -270)) && m_FacingRight == true)
            {
                if (m_Speed > m_MinSpeed)
                {
                    m_Speed = (m_Rigidbody2D.velocity.x - (0.001f * m_Rigidbody2D.rotation));
                }
                else
                {
                    m_Speed = m_MinSpeed;
                }
            }
            // if player if facing left and his rotation between 0->90 or -360->-270 then he's going down hill. Thus, his velocity should slowly increase over time.
            else if (((m_Rigidbody2D.rotation > 0 && m_Rigidbody2D.rotation <= 90) || (m_Rigidbody2D.rotation > -360 && m_Rigidbody2D.rotation <= -270)) && m_FacingRight == false)
            {
                if (m_Speed > m_MinSpeed)
                {
                    m_Speed = (m_Rigidbody2D.velocity.x + (0.001f * m_Rigidbody2D.rotation));
                }
                else
                {
                    m_Speed = m_MinSpeed;
                }
            }
            // if player if facing right and his rotation between 0->-90 or -360->270 then he's going up hill. Thus, his velocity should slowly decrease over time.
            else if (((m_Rigidbody2D.rotation < 0 && m_Rigidbody2D.rotation >= -90) || (m_Rigidbody2D.rotation < 360 && m_Rigidbody2D.rotation >= 270)) && m_FacingRight == true)
            {
                if (m_Speed > m_MinSpeed)
                {
                    m_Speed = (m_Rigidbody2D.velocity.x + (0.001f * m_Rigidbody2D.rotation));
                }
                else
                {
                    m_Speed = m_MinSpeed;
                }
            }
            // if player if facing right and his rotation between 0->-90 or -360->270 then he's going up hill. Thus, his velocity should slowly increase over time.
            else if (((m_Rigidbody2D.rotation < 0 && m_Rigidbody2D.rotation <= -90) || (m_Rigidbody2D.rotation < 360 && m_Rigidbody2D.rotation >= 270)) && m_FacingRight == false)
            {
                if (m_Speed > m_MinSpeed)
                {
                    m_Speed = (m_Rigidbody2D.velocity.x - (0.001f * m_Rigidbody2D.rotation));
                }
                else
                {
                    m_Speed = m_MinSpeed;
                    
                }
            }
        }
    }
}
