![UCU](https://github.com/ucudal/PII_Conceptos_De_POO/raw/master/Assets/logo-ucu.png)

## FIT - Universidad Católica del Uruguay

### Programación II

# API de Twitter

API para publicar en la cuenta de Twitter del curso [@POOUCU](https://twitter.com/pooucu) y enviar mensajes directos.

## Publicar en Twitter
Para poder publicar en Twitter, una vez agregada la libreria a su proyecto como referencia,
podrán hacer uso del siguiente código de ejemplo:

```c#
var twitter = new TwitterImage();
Console.WriteLine(twitter.PublishToTwitter("text", @"PathToImage.png"));
```

Esto publicará en la cuenta @POOUCU la imagen y texto enviados e imprime por consola el resultado de la publicación. En caso de ser correcta, imprime "OK".

## Mensajes privados
Twitter permite a los usuarios enviar mensajes privados entre ellos. Para esto, permitiremos enviar mensajes desde la cuenta de @POOUCU a un usuario cualquiera de Twitter.

Para ello, el usuario que desee recibir mensajes, debe primero admitir mensajes desde cualquiera en https://twitter.com/settings/safety o de lo contrario seguir a la cuenta @POOUCU.

Para enviar mensajes directos puedes utilizar el siguiente código:

```c#
var twitter = new TwitterMessage();
twitter.SendMessage("Hola!", "<userId>");
```

El userId puede ser encontrado utilizando por ejemplo [TwitterId](https://tweeterid.com/).

> Los repositorios que usan esta librería asumen que fueron descargados en la misma carpeta 'madre'.
