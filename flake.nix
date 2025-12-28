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
      dotnet_sdk_9 dotnet_runtime_9 avalonia gtk3 webkitgtk_4_1
      ];
    nativeBuildInputs = [ pkgs.pkg-config ];
    };

  };
}
