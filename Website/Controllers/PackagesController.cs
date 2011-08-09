﻿using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NuGet;

namespace NuGetGallery
{
    public class PackagesController : Controller
    {
        public const string Name = "Packages";

        // TODO: add support for URL-based package submission
        // TODO: add support for uploading logos and screenshots
        // TODO: improve validation summary emphasis

        readonly ICryptographyService cryptoSvc;
        readonly IPackageService packageSvc;
        readonly IPackageFileService packageFileSvc;
        readonly IUsersService usersSvc;

        public PackagesController(
            ICryptographyService cryptoSvc,
            IPackageService packageSvc,
            IPackageFileService packageFileRepo,
            IUsersService usersSvc)
        {
            this.cryptoSvc = cryptoSvc;
            this.packageSvc = packageSvc;
            this.packageFileSvc = packageFileRepo;
            this.usersSvc = usersSvc;
        }
        
        [ActionName(ActionName.SubmitPackage), Authorize]
        public ActionResult ShowSubmitPackageForm()
        {
            return View();
        }

        [ActionName(ActionName.SubmitPackage), Authorize, HttpPost]
        public ActionResult SubmitPackage(HttpPostedFileBase packageFile)
        {
            // TODO: validate package id and version don't already exist
            
            if (packageFile == null)
            {
                ModelState.AddModelError(string.Empty, "A package file is required.");
                return View();
            }

            // TODO: what other security checks do we need to perform for uploaded packages?
            var extension = Path.GetExtension(packageFile.FileName).ToLowerInvariant();
            if (extension != Const.PackageExtension)
            {
                ModelState.AddModelError(string.Empty, "The package file must be a .nupkg file.");
                return View();
            }

            // TODO: This should never be null, but should probably decide what happens if it is
            var currentUser = usersSvc.FindByUsername(User.Identity.Name);

            ZipPackage uploadedPackage;
            using (var uploadStream = packageFile.InputStream)
            {
                uploadedPackage = new ZipPackage(packageFile.InputStream);
            }

            Package packageVersion;
            try
            {
                packageVersion = packageSvc.CreatePackage(uploadedPackage, currentUser);
            }
            catch (EntityException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }

            return RedirectToRoute(
                RouteName.VerifyPackage, 
                new { id = packageVersion.PackageRegistration.Id, version = packageVersion.Version });
        }

        [ActionName(ActionName.VerifyPackage), Authorize]
        public ActionResult ShowVerifyPackageForm(
            string id,
            string version)
        {
            var package = packageSvc.FindByIdAndVersion(id, version);
            
            if (package == null)
                return HttpNotFound();

            return View(new VerifyPackageViewModel
            { 
                Id = package.PackageRegistration.Id,
                Version = package.Version,
                Title = package.Title,
                Summary = package.Summary,
                Description = package.Description,
                RequiresLicenseAcceptance = package.RequiresLicenseAcceptance,
                LicenseUrl = package.LicenseUrl,
                Tags = package.Tags,
                ProjectUrl = package.ProjectUrl,
            });
        }

        [ActionName(ActionName.VerifyPackage), Authorize, HttpPost]
        public ActionResult VerifyPackage(
            string id,
            string version)
        {
            // TODO: handle requesting to verify a package that is already verified; return 404?

            var package = packageSvc.FindByIdAndVersion(id, version);

            if (package == null)
                return HttpNotFound();

            packageSvc.PublishPackage(package);

            // TODO: add a flash success message

            return RedirectToRoute(RouteName.DisplayPackage, new { id = package.PackageRegistration.Id, version = package.Version });
        }

        [ActionName(ActionName.DisplayPackage)]
        public ActionResult DisplayPackage(
            string id,
            string version)
        {
            var package = packageSvc.FindByIdAndVersion(
                id,
                version);

            if (package == null)
                return HttpNotFound();

            return View(new DisplayPackageViewModel(
                package.PackageRegistration.Id,
                package.Version)
            {
                Description = package.Description,
                Authors = package.Authors.Flatten(),
            });
        }

        [ActionName(ActionName.ListPackages)]
        public ActionResult ListPackages()
        {
            var packageVersions = packageSvc.GetLatestVersionOfPublishedPackages();

            var viewModel = packageVersions.Select(pv => 
                new ListPackageViewModel
                {
                    Id = pv.PackageRegistration.Id,
                    Version = pv.Version,
                });
            
            return View(viewModel);
        }
    }
}