﻿using System.Linq;

namespace NuGetGallery
{
    public class UsersService : IUsersService
    {
        readonly ICryptographyService cryptoSvc;
        readonly IEntityRepository<User> userRepo;
        
        public UsersService(
            ICryptographyService cryptoSvc,
            IEntityRepository<User> userRepo)
        {
            this.cryptoSvc = cryptoSvc;
            this.userRepo = userRepo;
        }
        
        public virtual User Create(
            string username, 
            string password, 
            string emailAddress)
        {
            // TODO: validate input
            // TODO: add email verification workflow
            // TODO: consider encrypting email address with a public key, and having the background process that send messages have the private key to decrypt
            // TODO: allow the message system to use markdown for email bodies to make it easy to send text and HTML messages properly
            
            var existingUser = FindByUsername(username);
            if (existingUser != null)
                throw new EntityException(Strings.UsernameNotAvailable, username);

            existingUser = FindByEmailAddress(emailAddress);
            if (existingUser != null)
                throw new EntityException(Strings.EmailAddressBeingUsed, emailAddress);

            var hashedPassword = cryptoSvc.GenerateSaltedHash(password);

            var newUser = new User(
                username,
                hashedPassword,
                emailAddress);

            // TODO: enqueue a real message instead of one with a dummy subject and body
            newUser.Messages.Add(new EmailMessage(
                "theSubject",
                "theBody"));

            userRepo.InsertOnCommit(newUser);
            userRepo.CommitChanges();

            return newUser;
        }

        public virtual User FindByEmailAddress(string emailAddress)
        {
            // TODO: validate input
            
            return userRepo.GetAll()
                .Where(u => u.EmailAddress == emailAddress)
                .SingleOrDefault();
        }

        public virtual User FindByUsername(string username)
        {
            // TODO: validate input
            
            return userRepo.GetAll()
                .Where(u => u.Username == username)
                .SingleOrDefault();
        }

        public virtual User FindByUsernameAndPassword(
            string username,
            string password)
        {
            // TODO: validate input
            
            var user = FindByUsername(username);

            if (user == null)
                return null;

            if (!cryptoSvc.ValidateSaltedHash(user.HashedPassword, password))
                return null;

            return user;
        }
    }
}