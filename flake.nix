{
  description = "rust overwrite will be earler that GTA VI, i promise";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
  };

  outputs = { self, nixpkgs, }: let
    pkgs = nixpkgs.legacyPackages."x86_64-linux";
  in {
    devShells."x86_64-linux".default = pkgs.mkShell {
      buildInputs = with pkgs; [
      dotnetCorePackages.sdk_9_0_1xx-bin dotnetCorePackages.runtime_9_0-bin dotnetPackages.Nuget avalonia gtk3 webkitgtk_4_1
      ];
    nativeBuildInputs = [ pkgs.pkg-config ];
    };

  };
}
