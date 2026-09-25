BU KLASÖR (wwwroot\icons) ORDUNET İÇİN İKON VE LOGO DOSYALARINI BARINDIRIR.

Kendi logonuzu eklemek için:
1. İstediğiniz logo dosyasını (.png, .svg, .jpg vb.) bu klasöre kopyalayınız.
2. Örneğin dosya adınız 'adliye-logo.png' ise;
   Views/Shared/_Layout.cshtml içindeki 48. satırda bulunan:
   <img src="~/icons/logo.svg" ... />
   kısmını:
   <img src="~/icons/adliye-logo.png" ... />
   olarak güncelleyiniz.
