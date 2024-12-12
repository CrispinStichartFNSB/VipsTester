In Property Search, we're using [libvips](https://www.libvips.org/) to process TIFF images from [DNR's website](https://dnr.alaska.gov/). A lot of their images are weirdly formatted and can cause problems with image processing libraries, libvips included.

As of December 12th, 2024, there's been a regression in libvips. Version 15.3 can handle all the included images, but 15.5 and 16.0 crash on one of them. Before updating the version of libvips that Property Search uses, use this program to check that it works.  

Note that this repository is configured to use Git Large File Storage (LFS), because one of the test images is 107 MB, and without LFS GitHub can only handle up to 100MB. See [the LFS  documentation](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github) for more details.