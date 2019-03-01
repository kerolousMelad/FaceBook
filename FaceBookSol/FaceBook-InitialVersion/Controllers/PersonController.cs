﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FaceBook_InitialVersion.Data;
using FaceBook_InitialVersion.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceBook_InitialVersion.Controllers
{
    public class PersonController : Controller
    {
        private readonly ApplicationDbContext _db;

        [BindProperty]
        public PersonModelView personModelView { get; set; }

        public PersonController(ApplicationDbContext DB)
        {
            _db = DB;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Home(string UserName)
        {
            var posts = _db.Users.Where(P => P.UserName == UserName)
                                 .Select(P => P.Posts).ToList();

            return View(posts);
        }

        // GET
        public IActionResult Profile(string UserName)
        {
            
            if (UserName == null)
            {
                return NotFound();
            }

            #region Old Version
            //var currentPerson = _db.Users.Include(P => P.Posts)
            //                    .Include(P => P.MyRequests)
            //                    .Include(P => P.FriendsRequest)
            //                    .FirstOrDefault(P => P.UserName == UserName);

            //if (currentPerson == null)
            //{
            //    return NotFound();
            //}
            #region not used
            //var s = currentPerson.FriendRequestAccepted.Include(F => F.User);

            // var x = _db.Friendships.Where(F => F.User.UserName == Name).Include(F => F.Friend);

            // var t = _db.Entry(currentPerson).Collection(P => P.FriendRequestAccepted).Query().Include(P => P.User); 
            #endregion
            //// load friends data 
            //foreach (var item in currentPerson.FriendsRequest)
            //{
            //    item.User = _db.Users.FirstOrDefault(Us => Us.Id == item._userID);
            //}
            //foreach (var item in currentPerson.MyRequests)
            //{
            //    item.Friend = _db.Users.FirstOrDefault(Us => Us.Id == item._friendID);
            //} 
            #endregion

            personModelView = GetPersonModel(UserName);
            if (personModelView.CurrentUser == null)
            {
                return NotFound();
            }

            return View(personModelView);
        }

        #region EditInfo
        // Get 
        public async Task<IActionResult> EditInfo(string UserName)
        {
            if (UserName == null)
            {
                return NotFound();
            }

            var person = await _db.Users.FirstOrDefaultAsync(p => p.UserName == UserName);
            if (person == null)
            {
                return NotFound();
            }
            return View(person);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInfo(string UserName, Person person)
        {
            if (UserName == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    /// update Person 
                    //1- get person
                    var targetPerson = await _db.Users.FirstOrDefaultAsync(p => p.UserName == UserName);
                    if (targetPerson == null)
                    {
                        return NotFound();
                    }
                    // update data
                    targetPerson.Bio = person.Bio;
                    targetPerson.Gender = person.Gender;
                    targetPerson.FirstName = person.FirstName;
                    targetPerson.LastName = person.LastName;
                    targetPerson.BirthDay = person.BirthDay;

                    _db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_db.Users.Any(P => P.UserName == UserName))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Profile), new { @UserName = UserName });
            }
            return View(person);
        }

        #endregion

        #region Dealing with friends Request
        /// <summary>
        /// 
        /// </summary>
        /// <param name="UserID"> UserID of the Person who sent the request</param>
        /// <param name="FriendUserName"> userName of the Person who Received the request </param>
        /// <returns></returns>
        public IActionResult DeleteFriendRequest(string UserID, string FriendUserName)
        {
            // get friendship
            var friendship = GetFriendship(UserID, FriendUserName);
            if (friendship == null)
            {
                return NotFound();
            }

            _db.Friendships.Remove(friendship);
            _db.SaveChanges();
            return RedirectToAction(nameof(Profile), new { @UserName = FriendUserName });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="UserID"> UserID of the Person who sent the request</param>
        /// <param name="FriendUserName"> userName of the Person who Received the request </param>
        /// <returns></returns>
        public IActionResult ConfirmFriendRequest(string currentUserName, string FriendUserName)
        {
            // get friendship
            var friendship = GetFriendship(currentUserName, FriendUserName);
            if (friendship == null)
            {
                return NotFound();
            }

            //update friendship
            friendship.friendShipStatus = Enums.FriendShipStatus.Accepted;
            _db.SaveChanges();
            return RedirectToAction(nameof(Profile), new { @UserName = FriendUserName });
        } 
        #endregion

        #region  Dealing with Friends
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentUserName"> UserID of the Person who sent the request</param>
        /// <param name="friendUserName"> userName of the Person who Received the request </param>
        /// <returns></returns>
        public IActionResult RemoveFriend(string currentUserName, string friendUserName)
        {
            if (currentUserName == null && friendUserName == null)
            {
                return NotFound();
            }

            var friendship = _db.Friendships.FirstOrDefault(f =>
                ((f.User.UserName == currentUserName) && (f.Friend.UserName == friendUserName))
                ||
                ((f.Friend.UserName == currentUserName) && (f.User.UserName == friendUserName))
            );

            if (friendship != null)
            {
                _db.Friendships.Remove(friendship);
                _db.SaveChanges();
            }
            return RedirectToAction(nameof(Profile), new { @UserName = currentUserName });
        }

        public IActionResult AddFriend(string friendUserName)
        {
            if (friendUserName == null)
            {
                return NotFound();
            }

            Friendship friendship = new Friendship()
            {
                _userID = _db.Users.FirstOrDefault(S => S.UserName == User.Identity.Name)?.Id,
                _friendID = _db.Users.FirstOrDefault(S => S.UserName == friendUserName)?.Id,
                friendShipStatus = Enums.FriendShipStatus.Pending
            };

            _db.Friendships.Add(friendship);
            _db.SaveChanges();
            return RedirectToAction(nameof(Profile), new { @UserName = friendUserName });
        }
        #endregion

        private PersonModelView GetPersonModel(string userName)
        {

            var _currentUser = _db.Users.Where(P => P.UserName == userName)
                               .Include(P => P.Posts)
                               .Include(P => P.FriendsRequest)
                               .Include(P => P.MyRequests)
                               .FirstOrDefault();



            var friendsId = _currentUser.MyRequests
                                        .Where(Fr => Fr.friendShipStatus == Enums.FriendShipStatus.Accepted)
                                        .Select(P => P._friendID)
                                        .Union
                                        (
                                         _currentUser.FriendsRequest
                                         .Where(F => F.friendShipStatus == Enums.FriendShipStatus.Accepted)
                                         .Select(P => P._userID)
                                        );

            List<string> friendsRequestId = new List<string>();
            if (User.Identity.Name == userName)
            {
                friendsRequestId  = _currentUser.FriendsRequest
                                     .Where(F => F.friendShipStatus == Enums.FriendShipStatus.Pending).AsQueryable()
                                     .Select(f => f._userID).ToList();
            }


            PersonModelView modelView = new PersonModelView()
            {
                CurrentUser = _currentUser,
                MyPosts = _currentUser?.Posts,

                 // check the login person             
                FriendPosts = User.Identity.Name == userName ? // ternary operator
                             _db.Posts.Where(P => (friendsId.Contains(P.UserID)) && User.Identity.Name == userName).ToList() // true
                             : new List<Post>(),                                                                             // false

                myFriends = _db.Users.Where(U => (friendsId.Contains(U.Id))).ToList(),
                myFriendsRequest = User.Identity.Name == userName ? // ternary operator
                                  _db.Users.Where(U => (friendsRequestId.Contains(U.Id))).ToList()   // true
                                  : new  List<Person>()                                              // false
            };

            return modelView;
        }


        private Friendship GetFriendship(string currentUserName, string friendUserName)
        {
            if (currentUserName == null && friendUserName == null)
            {
                return null;
            }

            // in friend request to you mean => (user => how sent the request, friend => you (how received the request))
            return _db.Friendships.FirstOrDefault(F => F.Friend.UserName == currentUserName && F.User.UserName == friendUserName);
        }
    }
}