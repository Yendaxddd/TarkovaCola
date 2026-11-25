# Tarkova-Cola 4.0.1

**Perk-a-Colas de Call of Duty Zombies para SPT** · *Call of Duty Zombies Perk-a-Colas for SPT*

Compatible con / Compatible with: **SPT 4.1.x** (single player; no se garantiza que funcione con Fika / not guaranteed to work with Fika, report issues on the SPT page or GitHub).
Opcional / Optional: **VisitAPI 1.3.5+** (para la historia y las misiones / for the storyline and quests).

---

## Español

### Hotfix 4.0.1
- Compatibilidad con **Maps Expanded**: las máquinas de perks, Der Wunderfizz, la Caja Misteriosa, TEDD, el interruptor de la luz y las marcas de Death Perception también salen en su mapa. Aportado por **mattpsvreis** (PR #2, ¡gracias!).
- Texturas limitadas a 1024 para que la descarga pese menos (593 MB en vez de 1,3 GB).

### Novedades de la 4.0: La Última Ronda
La última gran actualización de Tarkova-Cola, y la más grande con diferencia: **6 perks nuevas** (15 en total), **6 armas más de Black Ops 7** (8 en total), animaciones de beber y de cuerpo a cuerpo de Black Ops, máquinas en **todos los mapas**, un estuche de perks, 45 logros nuevos, un repaso de equilibrio a todas las perks y compatibilidad con muchos mods populares. Además, todos los textos se pueden **traducir**.

> **Actualizando desde la 3.x:** extrae el zip encima de tu carpeta de SPT y sobrescribe. Tu progreso, tus perks eternas y tus perks del modo Clásico se conservan.

#### Resumen
| | |
|---|---|
| **Perks nuevas** | Quick Revive, Mule Kick, Electric Cherry, Death Perception, Tombstone Soda y Double Tap Root Beer, cada una con su árbol de Research, APEX, canción, botella e inyector |
| **Armas de Black Ops 7** | Peacekeeper Mk1, Voyak KT-3, MXR-17, VST, M10 Breacher y Hawker HX, con animaciones y sonidos de BO7. MK35 ISR y VMP también los reciben |
| **Animaciones** | Animación de beber de Black Ops 7 para todas las perks, y animaciones de cuerpo a cuerpo de Black Ops 7 para las 23 armas blancas de EFT |
| **Máquinas** | Máquinas, Caja Misteriosa, TEDD y el interruptor de la luz en **todos los mapas**, colocados solos. Con el editor de máquinas puedes poner las tuyas |
| **Caja Misteriosa** | Puede dar la Ray Gun, la Ray Gun Mark II y la Wunderwaffe DG-2 si tienes esos mods |
| **Estuche de perks** | Un estuche que solo guarda perks y núcleos APEX |
| **Logros** | 45 nuevos, 127 en total |
| **Clásico** | La perk modificadora da ahora el 50 % de su efecto más una mejora firma fija |
| **Compatibilidad** | SAM (Advanced Movement), Dynamic Maps, Maps Expanded, SAIN, QuestingBots, K9 Companion, PIT Fireteam, TacticalReload, Weapon Camo and Stickers y las armas maravilla de Kenner |
| **Traducciones** | Todos los textos se pueden traducir con archivos JSON (ver `TRANSLATING.md`) |

---

#### Las 6 perks nuevas
| Perk | Efecto base |
|---|---|
| **Quick Revive** | Tras 8 s sin recibir daño regeneras 1 PV por segundo en una parte herida a la vez (la cabeza y el tórax primero), también esprintando. La vida sale de uno de tus botiquines (nunca del que llevas en la mano): sin botiquín no hay regeneración. |
| **Mule Kick** | Una **3.ª ranura de arma** que admite cualquier arma principal. Se saca con la rueda del ratón. Cambiar a ella o desde ella es un 50 % más rápido. |
| **Electric Cherry** | Al terminar una recarga ganas un **impulso eléctrico de velocidad al esprintar** durante 1 s. Cuanto más vacío estaba el cargador, mayor (hasta +150 %). Funciona con todas las armas: cargadores, tubos de escopeta, revólveres y cerrojos. |
| **Death Perception** | Ves mejor en la oscuridad, y los enemigos que ves a menos de 20 m se marcan con un **contorno naranja** hasta que los pierdes de vista. |
| **Tombstone Soda** | Cuando un golpe te deja por debajo del 25 % de tu vida total, sueltas una **lápida señuelo** y te conviertes en **espectro** durante 4 s. Los bots te pierden y disparan a la lápida, y tú te mueves un 30 % más rápido pero no puedes disparar. Como mucho una vez cada 3 minutos. |
| **Double Tap Root Beer** | Cadencia ×1.10, y cada acierto seguido en menos de 0.4 s la sube otro 3 % (hasta +15 %). Fallar o dejar de disparar reinicia el ritmo. |

**Cómo se consiguen:** Therapist (nivel de lealtad 1, 7 monedas GP cada una), botín en raid (los mismos sitios que las demás perks), la máquina **Der Wunderfizz**, las pruebas de TEDD y la perk aleatoria. No dependen de la historia.

Como todas las perks, cada una sube del nivel 1 al 5, revela desafíos y desbloquea **3 mejoras mayores, 4 menores y una Ranura extra**, cada una con su desventaja (3 mayores, 4 menores).

##### Quick Revive
| Tipo | Mejora | Efecto |
|---|---|---|
| Mayor | **Revivir** | Necesita un estimulante en el inventario (lo gasta). Un golpe letal te deja **Caído** 10 s a 1 PV: te quitan el arma, solo tienes una pistola y vas arrastrándote. Si matas a un enemigo te levantas con el 30 % de vida y conservas tus perks; si no, mueres. Una vez por raid. |
| Mayor | **Transfusión** | La regeneración recupera las partes destruidas (vuelven a 1 PV), pero no les da más vida: desde ahí solo las curan los medicamentos. |
| Mayor | **Médico de combate** | Recibir daño ya no corta la regeneración: en combate curas 0.3 PV/s. |
| Menor | **Paciencia** | La regeneración empieza tras 5 s sin daño (en vez de 8). |
| Menor | **Prioridad** | La cabeza y el tórax se regeneran un 50 % más rápido. |
| Menor | **Coagulación** | Mientras regeneras, los sangrados leves se cortan. |
| Menor | **Reserva** | Al recibir daño curas 2 PV/s durante 3 s; luego se para hasta que pase la espera normal. |
| Desventaja mayor | Sangre fina · Metabolismo caro · Sobresalto | Espera 14 s · cada PV cuesta 0.5 de energía y 0.5 de hidratación · tus propios disparos reinician la espera |
| Desventaja menor | Regeneración tibia · Cicatrices · Sin prisas · Cierre débil | −25 % de curación · solo cura hasta el 80 % · se para al esprintar · +10 % de daño recibido mientras regeneras |
| APEX | Segunda vida | Revivir se recarga cada 7 bajas de PMC (sigue necesitando un estimulante). |

##### Mule Kick
| Tipo | Mejora | Efecto |
|---|---|---|
| Mayor | **Coz de mula** | El primer disparo en los 2 s siguientes a cambiar de arma hace un 50 % más de daño. |
| Mayor | **Cartuchera de mula** | El arma de Mule Kick se recarga sola mientras está guardada (1 bala cada 1.5 s, de la munición suelta que llevas). |
| Mayor | **Cambio rápido** | Cambiar de arma con el cargador vacío es instantáneo. |
| Menor | **Correa ligera** | El arma de Mule Kick pesa la mitad. |
| Menor | **Recámara lista** | El arma de Mule Kick siempre sale con bala en la recámara. |
| Menor | **Equilibrio** | Durante 2 s después de sacar el arma de Mule Kick, el balanceo baja un 15 %. |
| Menor | **Funda** | Cambiar entre tus otras armas también es un 30 % más rápido. |
| Desventaja mayor | Carga de mula · Correa suelta · Cambio torpe | el arma pesa un 50 % más · un golpe de 30+ de daño tiene un 5 % de tirarla al suelo · cambiar a ella o desde ella es un 30 % más lento |
| Desventaja menor | Recámara vacía · Seguro puesto · Traqueteo · Enredado | sale sin bala en la recámara · el primer disparo se retrasa 0.3 s · sacarla hace ruido (15 m) · no puedes recargarla esprintando |
| APEX | Coz perfecta | Al cambiar de arma, la que sacas recibe bala en la recámara y 5 balas en el cargador. |

- La ranura solo admite armas con la perk activa. Si la perk termina con un arma dentro, la ranura sigue visible para que puedas sacarla.
- La rueda del ratón se puede desactivar en F12 (*Mule Kick: la rueda del ratón cambia de arma*).
- Convive con la funda extra de DualWieldLab.

##### Electric Cherry
| Tipo | Mejora | Efecto |
|---|---|---|
| Mayor | **Descarga** | Al recargar también sueltas una descarga eléctrica a tu alrededor: aturde a los enemigos hasta 1.5 s a 2-5 m, según lo vacío que estaba el cargador. |
| Mayor | **Sobrecarga** | Recargar durante el impulso lo acumula (hasta +225 %) y renueva su duración. |
| Mayor | **Relámpago** | Durante el impulso, esprintar no gasta estamina y puedes recargar esprintando. |
| Menor | **Batería** | El impulso dura 2 s (en vez de 1). |
| Menor | **Voltaje** | Cualquier recarga da al menos +45 % de velocidad. |
| Menor | **Realimentación** | Una baja durante el impulso lo devuelve a su valor inicial. |
| Menor | **Corriente estable** | El impulso no se desvanece: se mantiene al máximo y se corta al final. |
| Desventaja mayor | Fuga · Polaridad · Contragolpe | dura 0.5 s · solo cuentan las recargas con el cargador al menos a la mitad · un 15 % más lento durante 1.5 s al terminar |
| Desventaja menor | Cortocircuito · Chispa visible · Hormigueo · Techo bajo | recibir daño lo cancela · los bots te oyen chisporrotear (20 m) · +15 % de balanceo · impulso máximo +105 % |
| APEX | Tormenta | Cada recarga da el impulso máximo y suelta la descarga eléctrica. |

##### Death Perception
| Tipo | Mejora | Efecto |
|---|---|---|
| Mayor | **Sexto sentido** | Ves a través de las paredes: los enemigos a tu alcance se marcan aunque no los veas. |
| Mayor | **Ojo de cazador** | Las marcas no tienen límite de distancia (en vez de 20 m). |
| Mayor | **Instinto asesino** | Los enemigos ya no se marcan. A cambio, un aviso te dice cuándo un enemigo te mira por la espalda, y cada baja deja un **pulso UAV** en el cuerpo durante 30 s que revela a través de las paredes a los enemigos a 15 m de él. |
| Menor | **Persistencia** | La marca dura 3 s después de perder de vista al enemigo. |
| Menor | **Ojos de gato** | Visión nocturna más fuerte, y las cegadoras te ciegan la mitad. |
| Menor | **Sentido de metralla** | Las granadas enemigas se marcan con su trayectoria y su radio de peligro. |
| Menor | **Lectura** | El color del contorno indica la vida del enemigo. |
| Desventaja mayor | Interferencia · Vista corta · Paranoia | los contornos parpadean · las marcas solo llegan a 12 m · a veces aparecen marcas falsas |
| Desventaja menor | Retraso · Deslumbramiento · Migraña · Sobrecarga | las marcas se actualizan una vez por segundo · las cegadoras te ciegan un 30 % más · cada marca cuesta 2 de energía · +15 % de balanceo con 3+ marcas |
| APEX | Ojo que todo lo ve | Las marcas duran 5 s sin línea de visión y muestran la vida del enemigo. |

- El contorno es **solo el borde**, fino y translúcido, para no tapar al enemigo. Su opacidad está en F12 (*Opacidad del contorno*).
- La visión nocturna sube el brillo cuando la escena está oscura, sin el tinte verde de la visión nocturna.

##### Tombstone Soda
| Tipo | Mejora | Efecto |
|---|---|---|
| Mayor | **Ofrenda** | Si mueres, lo que llevas en los bolsillos y el chaleco vuelve por correo (una vez por raid). |
| Mayor | **Poltergeist** | La lápida estalla al terminar el espectro y aturde 2 s a los enemigos a 5 m. |
| Mayor | **Aparición** | Puedes disparar siendo espectro: el primer disparo hace el doble de daño y termina el espectro. |
| Menor | **Aparición larga** | El espectro dura 6 s (en vez de 4). |
| Menor | **Inquieto** | Tombstone se recarga en 2 minutos (en vez de 3). |
| Menor | **Ectoplasma** | Al volverte espectro se cortan los sangrados y curas 15 PV. |
| Menor | **Rastro frío** | Al terminar el espectro, los bots tardan 3 s más en encontrarte. |
| Desventaja mayor | Aparición corta · Alma pesada · Marcado | dura 2 s · se recarga en 5 minutos · al terminar, el bot más cercano sabe dónde estás |
| Desventaja menor | Frío · Lamento · Fantasma lento · Lápida frágil | sin estamina al terminar · un ruido que se oye a 40 m · sin bonus de velocidad · la lápida atrae los disparos solo 1.5 s |
| APEX | Más allá | Cada baja de PMC recarga Tombstone, y el espectro dura 6 s. |

##### Double Tap Root Beer
| Tipo | Mejora | Efecto |
|---|---|---|
| Mayor | **Doble disparo** | Con el ritmo al máximo, cada acierto tiene un 15 % de contar como dos balas (×2 de daño) sin gastar una bala extra. |
| Mayor | **Perforación** | El ritmo también sube la penetración (hasta +8 al máximo), solo contra blindaje de clase 4 o menos. Ideal para subfusiles y pistolas. |
| Mayor | **Inercia** | Fallar ya no reinicia el ritmo; solo dejar de disparar 1 s. |
| Menor | **Gatillo ligero** | Las armas semiautomáticas también suben el ritmo con cada acierto. |
| Menor | **Cañón frío** | La cadencia extra no calienta el arma. |
| Menor | **Alimentación** | La cadencia extra no sube los fallos del arma. |
| Menor | **Metrónomo** | El ritmo llega hasta +25 % (en vez de +15 %). |
| Desventaja mayor | Cargador hambriento · A destiempo · Atasco | con el ritmo al máximo el 5 % de los disparos gasta 2 balas · fallar te frena un 10 % durante 1 s · +50 % de fallos con el ritmo al máximo |
| Desventaja menor | Arranque lento · Patada · Desgaste · Irregular | los 3 primeros disparos un 10 % más lentos · +10 % de retroceso con el ritmo al máximo · +20 % de desgaste del arma · el bonus varía ±10 % |
| APEX | Redoble | El ritmo siempre empieza al máximo. |

---

#### Perks en enemigos y compañeros
Los enemigos que llevan una perk tienen su efecto, y los compañeros de PIT Fireteam tienen las perks que les equipas en sus 4 ranuras. Un aviso te dice cuándo salta la perk de un enemigo, y los iconos de sus perks flotan sobre su cabeza (con línea de visión, a menos de 60 m; F12 > *Iconos de perks sobre los enemigos*).

| Perk | En un enemigo |
|---|---|
| Juggernog | Recibe la mitad de daño |
| Speed Cola | Recarga ×1.5 más rápido |
| Stamin-Up | Estamina infinita e inmune al dolor |
| PHD Flopper | Inmune a explosiones y caídas |
| Widow's Wine | Sus disparos te frenan; sus granadas te atrapan en telarañas (8 m) |
| Deadshot | Sabe dónde están sus enemigos a través de las paredes (80 m) y +30 % de penetración |
| Bandolier Bandit | Aparece con munición extra |
| Vulture Aid | Aparece con un objeto valioso |
| Melee Macchiato | Cuerpo a cuerpo ×2. Recibe un 80 % menos de daño hasta que le golpeas cuerpo a cuerpo, y un disparo a la cabeza o al tórax no lo tumba antes que el resto de su cuerpo |
| **Quick Revive** | Se regenera. Un golpe letal puede dejarlo **caído** en vez de muerto: remátalo de un tiro a la cabeza, o se levantará más tarde con toda la vida e irá a por ti |
| **Mule Kick** | Aparece con una 3.ª arma, y cambia de arma en vez de recargar |
| **Electric Cherry** | Al recargar suelta una descarga (3 m) que te hace daño y te aturde |
| **Death Perception** | Solo te caza a ti y sabe más o menos dónde estás |
| **Tombstone Soda** | Suelta una granada activa al morir (3 s para alejarte) |
| **Double Tap** | Dispara un 25 % más rápido |

Todo se puede desactivar en F12 > *Efectos de perks en enemigos y compañeros*.

---

#### Armas de Black Ops 7
Seis armas nuevas, portadas con sus **propios modelos, animaciones en primera persona y sonidos** de Black Ops 7, montadas sobre un arma de EFT para que acepten sus accesorios y camuflajes. **MK35 ISR** y **VMP** ahora también tienen sus animaciones y sonidos de BO7.

| Arma | Tipo | Basada en | Precio |
|---|---|---|---|
| **MK35 ISR** | Fusil de asalto | HK416A5 (5.56) | |
| **VMP** | Subfusil | MP5 Navy (9×19) | |
| **Peacekeeper Mk1** | Fusil de asalto | AK-12 (5.45) | 120.000 ₽ |
| **Voyak KT-3** | Fusil de asalto | AK-74M (5.45) | 105.000 ₽ |
| **MXR-17** | Fusil de asalto | SCAR-L (5.56) | 115.000 ₽ |
| **VST** | Subfusil | MP9 (9×19) | 85.000 ₽ |
| **M10 Breacher** | Escopeta de corredera | M590 (12ga) | |
| **Hawker HX** | Francotirador de cerrojo | AXMC (.338 LM), con mira S&B | |

- **Cómo se consiguen:** Mechanic (nivel de lealtad 1), ya montadas. MK35 ISR y VMP también salen de la Caja Misteriosa.
- Miras de hierro utilizables, iconos correctos y el mismo tamaño que sus equivalentes reales.
- Compatibles con **Weapon Camo and Stickers**.

#### Animaciones de Black Ops
- **Beber:** todas las perks se beben con la animación de Black Ops 7 (la lata en la mano, la anilla, el trago). Activada por defecto; F12 > *Animación de beber de Black Ops*. El tamaño de la lata en la mano también está en F12.
- **Cuerpo a cuerpo:** las 23 armas blancas de EFT usan animaciones de Black Ops 7 (cuchillos, machetes, hachas, palancas, mazas...). F12 > *Animaciones de cuerpo a cuerpo de Black Ops*; desactívala para volver a los estilos de combate.

---

#### Máquinas en todos los mapas
- Las máquinas de perks, Der Wunderfizz, los puntos de la Caja Misteriosa, los de TEDD y el **interruptor de la luz** aparecen ahora en **todos los mapas**. Los mapas sin puntos colocados a mano los reciben **solos**: junto a paredes, cerca del botín, sobre la malla de navegación y repartidos.
  - F12 > Máquinas > *Colocación automática de máquinas*: **Fija** (se buscan una vez y se guardan), **Cada raid** (sitios nuevos en cada raid) o **Desactivada**.
  - F12 > Máquinas > *Rehacer máquinas automáticas*: la siguiente raid descarta los puntos automáticos de ese mapa y busca otros.
- **Editor de máquinas** (F12 > Máquinas, o **Fin** en raid): pon tus propias piezas donde quieras. Las piezas colocadas a mano siempre ganan a las automáticas.

  | Tecla | Acción |
  |---|---|
  | RePág / AvPág | Pieza siguiente / anterior (máquinas de perks, Der Wunderfizz, punto de la Caja Misteriosa, punto de TEDD, interruptor de la luz) |
  | [ / ] | Girar |
  | \\ | Ajustar el giro a 45° |
  | Insert | Colocar |
  | Supr | Borrar la pieza más cercana |
  | F11 | Captura de pantalla |

- **Der Wunderfizz** es la máquina de las 9 perks sin máquina propia (Bandolier, Vulture Aid, Melee Macchiato y las 6 nuevas).
- Las máquinas son más fáciles de usar: 1.6 m de alcance y más tolerancia a las diferencias de altura.
- **Dynamic Maps** o **Maps Expanded** muestra las máquinas y el interruptor en su mapa (ver Compatibilidad). Se usa el que esté instalado; ninguno es obligatorio.

#### Caja Misteriosa
- **Armas maravilla:** con la **Ray Gun**, la **Ray Gun Mark II** o la **Wunderwaffe DG-2** de Kenner instaladas, la caja tiene un 10 % de dar una de ellas, cargada igual que en su compra de pared.
- Armas de Black Ops 7: 15 %.

#### Estuche de perks
- Un estuche (2×2 por fuera, 5×4 por dentro) que solo admite botellas de perks, inyectores de perks y núcleos APEX.
- Therapist (nivel de lealtad 1, 65.000 ₽) y el mercadillo.

#### APEX
- Las 6 perks nuevas tienen su APEX (ver cada perk arriba).
- **APEX de Stamin-Up:** la estamina se regenera ×3 (antes era infinita).
- **APEX de Vulture Aid:** el 15 % de los enemigos (antes el 50 %), y nunca LEDX, tarjetas gráficas ni Bitcoin.
- **APEX de Bandolier:** su desafío pide 40 bajas (antes 60).
- Los **núcleos APEX** dejan de absorberse cuando ya no quedan desafíos por empezar, así que puedes venderlos.

#### Modo Clásico
- La 5.ª perk (el **modificador**) da ahora el **50 %** de su efecto base, más su **mejora menor firma** fija:

  | Perk | Firma | Perk | Firma |
  |---|---|---|---|
  | Speed Cola | Malabarista | Bandolier Bandit | Cañón frío |
  | Stamin-Up | Pies ligeros | Vulture Aid | Vista aguda |
  | Juggernog | Piel gruesa | Melee Macchiato | Desenfunde rápido |
  | PHD Flopper | Suelas suaves | Quick Revive | Paciencia |
  | Widow's Wine | Hilandero rápido | Mule Kick | Recámara lista |
  | Deadshot | Mira rápida | Electric Cherry | Batería |
  | Death Perception | Persistencia | Tombstone Soda | Aparición larga |
  | Double Tap | Cañón frío | | |

- El modificador también puede ganar una 2.ª mejora menor con el desafío de la raid.

#### Logros
- **45 logros nuevos**, 5 por cada perk añadida desde la 3.0 (Bandolier, Vulture Aid, Melee Macchiato y las 6 nuevas): primer trago, progreso total, nivel 5, todos los desafíos y uno secreto. **127** en total.
- Sonido nuevo al completar un desafío: la subida de rango de Black Ops 3.

---

#### Compatibilidad con otros mods
Todo es opcional y se detecta solo: sin estos mods no cambia nada.

| Mod | Qué hace Tarkova-Cola con él |
|---|---|
| **SAM (Sage's Advanced Movement)** | Con **PHD Flopper**, aterrizar una plancha provoca una explosión donde caes (el clásico *dolphin dive*; nunca te hace daño a ti ni a tu armadura). El deslizamiento es **el doble de rápido** y llega un 50 % más lejos, sin la cola lenta, con estela de energía rosa, chispas y sonido. Agacharte otra vez lo termina al instante, y sales de él esprintando a toda velocidad. |
| **Dynamic Maps** / **Maps Expanded** | Opcional. Si está instalado uno de los dos, su mapa y su minimapa muestran las máquinas de perks (con el icono de su perk), Der Wunderfizz, la Caja Misteriosa, TEDD y el interruptor de la luz, y los enemigos marcados por **Death Perception**, en directo. Los marcadores se añaden en caliente; Tarkova-Cola carga igual si no hay ninguno de los dos. Esos dos mods de mapa no pueden ir juntos. |
| **SAIN** | Los bots aturdidos (Widow's Wine, Electric Cherry, Tombstone) dejan de disparar mientras dura el aturdimiento. |
| **QuestingBots** | Los guardias y jefes de la historia se quedan en su puesto en vez de irse a hacer misiones. |
| **K9 Companion** | Tu perro no recibe daño de las explosiones de las perks (plancha de PHD, APEX). |
| **PIT Fireteam** | Los compañeros usan las perks de sus 4 ranuras, y las perks nunca los aturden, marcan ni dañan. |
| **Aliados en general** | Cualquier bot que no te sea hostil (por ejemplo, los Scavs cuando juegas de Scav) nunca es aturdido, marcado ni dañado por tus perks. Si te ataca, deja de contar como aliado. |
| **Ray Gun, Ray Gun Mark II, Wunderwaffe DG-2** (Kenner) | Pueden salir de la Caja Misteriosa. |
| **TacticalReload** | Los 2 cartuchos por inserción de Speed Cola respetan la capacidad del tubo junto con sus recargas de varios cartuchos (Benelli M3). |
| **MagRetention, Kirisame Mag Reload, StripperClips, Speedloader** | Speed Cola y Electric Cherry funcionan con sus recargas. |
| **Weapon Camo and Stickers** | Los camuflajes y pegatinas se aplican a las armas de Black Ops 7. |
| **DualWieldLab** | La ranura de Mule Kick convive con su funda extra. |
| **Arena Mode** | Compatible desde la 2.2 (el modo Zombis suelta perks). |

#### Traducciones
- **Todos los textos** del mod se pueden traducir con archivos JSON normales: las pantallas de Research, APEX y Clásico, avisos, perks, mejoras, máquinas, opciones de F12, objetos, logros y misiones.
- El mod escribe **plantillas** al día (`en.json` y `es.json`) cada vez que arranca, en `BepInEx/plugins/TarkovaCola/locales/` y `SPT_Runtime/user/mods/TarkovaCola/locales/`.
- Copia `en.json` con el código de tu idioma (`ch`, `ru`, `fr`, `de`, `jp`...), traduce los valores y reinicia. F12 > *Idioma* lista todas las traducciones instaladas.
- La guía completa está en **`TRANSLATING.md`**. Manda tu traducción y se incluirá con tu crédito.

#### Otros cambios
- **Speed Cola en armas manuales:** las escopetas de corredera, los rifles de cerrojo y los revólveres recargan **el doble de rápido** y accionan el cerrojo o la corredera el doble de rápido. Las escopetas de tubo cargan **2 cartuchos por inserción**.
- **PHD Flopper:** las explosiones ya no desgastan tu armadura.
- **F12 > Audio:** volumen separado para las canciones de las perks y para los efectos de sonido.
- **F12 > Perks > Pausar efectos de las perks:** apaga todos los efectos sin perder tus perks, para comparar.
- **Bandolier y Widow's Wine** tienen sus propias latas, y los iconos de las latas en el inventario ya no salen blancos.
- Las perks no se pueden beber en el refugio.
- Las pantallas de **Research**, APEX, Eternas y ESC caben las 15 perks.

#### Repaso de equilibrio
**Desafíos que se podían hacer trampa**
- El desafío de absorber de PHD Flopper solo cuenta las caídas que de verdad te harían daño.
- El desafío de registrar de Vulture Aid pide abrir el contenedor o el cuerpo.
- Varios desafíos piden ahora tener su perk activa.

**Objetivos más bajos**

| Mejora | Desafío nuevo |
|---|---|
| Stamin-Up: Hora punta | 3 bajas en menos de 3 minutos, 3 veces |
| Widow's Wine: Nido | Atrapa a 3 enemigos con una granada, 5 veces |
| Melee Macchiato: Golpe relámpago | 100 ataques cuerpo a cuerpo |
| Melee Macchiato: Carga | Corre 1 km con un arma cuerpo a cuerpo |
| Deadshot: Hacia el cielo | Dispara 800 balas |
| Bandolier: Cañón frío | Dispara 600 balas |
| Juggernog: Placas de hueso | 40 impactos en tu armadura |
| Juggernog: Gorro de calavera | Recibe 600 de daño |
| Stamin-Up: Pies ligeros · PHD: Caminante del terreno | 3 km |

**Efectos**
- **Dead Aim:** balanceo ×0.35 en vez de nada.
- **Segundo aliento:** 90 s de espera.
- **Cráneo de hierro:** una vez por raid.
- **Radio de la telaraña:** 8 m.

**Desventajas que ahora afectan a su propia perk**

| Perk | Desventaja | Efecto |
|---|---|---|
| Juggernog | **Armadura fría** | La reducción de daño empieza en 0 con el primer golpe de un combate y tarda 3 s en llegar al máximo. |
| PHD Flopper | **Onda propia** | Las explosiones no te hacen daño, pero te sacuden: +30 % de balanceo durante 2 s. |
| Deadshot | **Ojo cansado** | Tras 8 s seguidos apuntando, la reducción de balanceo baja a la mitad. |
| Vulture Aid | **Buitre exigente** | Solo se marcan los contenedores y cuerpos que valen 100.000 ₽ o más. |
| Melee Macchiato | **Filo fugaz** | El daño ×2 solo se aplica al primer golpe a cada enemigo. |
| Speed Cola | **Recarga ruidosa** | Cada recarga con Speed Cola hace ruido que oyen los bots a 15 m. |

**Eliminado**
- **Sinergias.** Sus logros se ocultan; el resto de tus logros no se tocan.
- Las herramientas de desarrollo de F12 (copiar posición, grabar clip, depuración de cuerpo a cuerpo), y las opciones *Estilos de cuerpo a cuerpo* y *Centrar armas ligeras*: los estilos están siempre activos cuando las animaciones de Black Ops están desactivadas.

---

### Qué es
Bebidas que encuentras, compras o fabricas y que te dan una habilidad durante la raid. Cuanto más las usas, más sube la perk: se revelan desafíos y, al completarlos, desbloqueas **mejoras** (y sus **desventajas** ligadas) que eliges en la pantalla **Research**.

### Las perks

| Perk | Efecto base (el resto de la raid) | Máquina |
|---|---|---|
| **Speed Cola** | Recargas ×1.5 más rápidas (×2 y 2 cartuchos por inserción en armas manuales) | Propia |
| **Stamin-Up** | Estamina de piernas ×1.5 | Propia |
| **Juggernog** | −33 % de daño recibido | Propia |
| **PHD Flopper** | Inmune al daño de caídas y explosiones, y las explosiones no desgastan tu armadura | Propia |
| **Widow's Wine** | Tus granadas atrapan a los enemigos en telarañas (−60 % de velocidad) | Propia |
| **Deadshot Daiquiri** | Apuntas un 15 % más rápido y el balanceo baja un 25 % | Propia |
| **Bandolier Bandit** | Los cargadores del chaleco y los bolsillos se rellenan con la munición suelta que llevas | Der Wunderfizz |
| **Vulture Aid Elixir** | Los contenedores y cuerpos sin registrar con objetos valiosos brillan a través de las paredes (15 m) | Der Wunderfizz |
| **Melee Macchiato** | Daño cuerpo a cuerpo ×2 y ataques un 30 % más rápidos | Der Wunderfizz |
| **Quick Revive** | Regeneras 1 PV/s tras 8 s sin daño, a costa de tus botiquines | Der Wunderfizz |
| **Mule Kick** | Una 3.ª ranura de arma, y cambios más rápidos a ella y desde ella | Der Wunderfizz |
| **Electric Cherry** | Un impulso de velocidad al esprintar tras cada recarga (hasta +150 %) | Der Wunderfizz |
| **Death Perception** | Visión nocturna y un contorno en los enemigos que ves (20 m) | Der Wunderfizz |
| **Tombstone Soda** | Por debajo del 25 % de vida, una lápida señuelo y 4 s como espectro | Der Wunderfizz |
| **Double Tap Root Beer** | Cadencia ×1.10, hasta un +15 % más con aciertos seguidos | Der Wunderfizz |

Cada perk sube del nivel 1 al 5 con el XP de las raids en las que la llevas (+25 %). Cada nivel revela desafíos; cada desafío desbloquea una mejora y su desventaja. Equipas 1 mejora mayor + 1 menor (2 con la Ranura extra) y **cada mejora necesita su desventaja equipada** (mayor con mayor, menor con menor).

**Dónde se consiguen:** Therapist (monedas GP), botín en raid (cajas fuertes, cuerpos de PMC, maletines médicos, cajas de armas, sitios de bebidas sueltas...), la Unidad de nutrición del refugio (recetas de la historia), las máquinas de todos los mapas, TEDD, la perk aleatoria y los enemigos que las llevan.

### Modos (interruptor en Research)
- **Moderno**: Research completo, todas las perks que quieras.
  - **Eternal Perk**: si bebes una perk que ya llevas, se vuelve **eterna** (contorno azul): se queda contigo entre raids, pierde una desventaja y, si mueres, su botella vuelve por correo. Pestaña **ETERNAS** en Research.
- **Clásico**: sin Research ni desventajas. Hasta 4 perks + 1 **modificador** (el 50 % de su efecto más su mejora firma). Cada perk trae 1 mejora menor al azar y **se quedan contigo hasta que mueras**. Cada raid trae un desafío opcional que da una 2.ª mejora.
- **APEX** (en los dos modos): un efecto muy fuerte por perk, que se desbloquea con núcleos APEX y desafíos.

| APEX | Efecto |
|---|---|
| Juggernog | Inmune al cuerpo a cuerpo y sin sangrados |
| Speed Cola | Tu arma nunca se encasquilla |
| Stamin-Up | La estamina se regenera ×3 y +10 % de velocidad al esprintar |
| PHD Flopper | Cada baja provoca una explosión (no te hace daño) |
| Widow's Wine | Tus explosivos estallan solos cuando un enemigo pasa cerca (3 m) |
| Deadshot | +10 de penetración (una clase de armadura más) |
| Bandolier | Una baja rellena tu cargador |
| Vulture Aid | El 15 % de los enemigos lleva un objeto valioso o de misión |
| Melee Macchiato | No recibes daño mientras golpeas |
| Quick Revive | Revivir se recarga cada 7 bajas de PMC |
| Mule Kick | El arma que sacas recibe bala en la recámara y 5 balas |
| Electric Cherry | Cada recarga da el impulso máximo y la descarga eléctrica |
| Death Perception | Las marcas duran 5 s sin línea de visión y muestran la vida |
| Tombstone Soda | Las bajas de PMC la recargan, y el espectro dura 6 s |
| Double Tap | El ritmo siempre empieza al máximo |

### En el mapa
- **Máquinas de perks** (30.000 ₽, o 15.000 ₽ para hacer eterna una perk que ya llevas), **Der Wunderfizz** (perk al azar, 40.000 ₽), la **Caja Misteriosa** (1.000 ₽: mantén F para un arma, Y para un accesorio) y **TEDD** (pruebas al azar que dan una perk), en todos los mapas. Primero enciende el **interruptor de la luz**.
- **Perk aleatoria:** al matar hay un 4 % de que caiga una **botella flotante** (como mucho una por raid, 60 s). Recógela: perk al azar, o eterna si ya la llevabas. Con su voz de locutor.

### Historia: los Embotelladores (requiere VisitAPI)
10 misiones de **Skier** y **Therapist** (desde el nivel 12):
- **3 de introducción**: desbloquean las **recetas** de las perks en la **Unidad de nutrición** del refugio.
- **6, una por perk original**: desbloquean la **venta** de esa perk en Therapist. Los guardias llevan la perk de la misión de verdad (y su icono sobre la cabeza).
- **Final**: el **Barman**, con todas las perks (4 en modo Clásico).

Con VisitAPI instalado, **las 6 perks originales no se venden en Therapist hasta completar su misión** (se siguen encontrando en raid y fabricando). Las 9 perks más nuevas se venden siempre. Sin VisitAPI no hay misiones y todo se vende desde el principio. Opción de depuración en F12: *Perks a la venta sin misiones*.

### Logros
**127 logros nativos** de EFT (Perfil > Logros), con recompensas por correo.

### Cómo se usa
1. Consigue una perk (Therapist, raid, refugio, máquinas).
2. Menú principal: botón **RESEARCH** (barra inferior) o **F9** para ver el progreso, equipar mejoras y cambiar de modo.
3. Bébela en raid. Con **ESC** en raid ves tu nivel y tus desafíos.

### Instalación
Extrae el zip **encima de la carpeta de SPT** (donde está `EscapeFromTarkov.exe`). Crea:
- `BepInEx/plugins/TarkovaCola/` (cliente)
- `SPT_Runtime/user/mods/TarkovaCola/` (servidor)
- `SPT_Runtime/user/mods/VisitAPI-Server/packs/TarkovaCola/` (misiones; solo hacen algo si tienes VisitAPI)

El progreso se guarda por perfil en `user/mods/TarkovaCola/profiles/` (no lo borres al actualizar). **Actualizando desde la 1.x, 2.x o 3.x:** tu progreso se conserva.

### Configuración (F12)
- **General:** idioma, tecla de Research, abandonar la raid no cuenta como muerte, *Pausar efectos de las perks*.
- **HUD:** iconos de perks abajo en el centro, iconos de perks sobre los enemigos, aviso de portadores de perks.
- **Audio:** volumen de canciones, voces, canciones de las perks y efectos de sonido.
- **Visual:** efectos de PHD y de la telaraña, opacidad del contorno de Death Perception, animaciones de beber y de cuerpo a cuerpo de Black Ops, tamaño de la lata en la mano.
- **Perks:** probabilidad de la perk aleatoria, perks como inyectores, tecla de re-roll de Vulture Aid, rueda del ratón de Mule Kick, efectos de perks en enemigos y compañeros.
- **Máquinas:** interruptor de la luz, colocación automática (Fija / Cada raid / Desactivada), rehacer máquinas automáticas, editor de máquinas, tecla de accesorio de la Caja Misteriosa, probabilidad de TEDD.
- **Arena** y **Depuración** (guardias siempre, perks a la venta sin misiones, registro detallado).

### Desinstalación
Borra las carpetas `TarkovaCola` (y el pack de VisitAPI). Si te quedan perks en el alijo, SPT permite eliminar los items rotos.

**Mule Kick:** antes de desinstalar, saca el arma de la ranura de Mule Kick (3.ª ranura de arma) al alijo. Sin el mod esa ranura deja de existir y SPT tratará el arma como un objeto roto.

<details>
<summary><b>Versiones anteriores (3.0.1 y antes)</b></summary>

#### Hotfix 3.0.1
- **Núcleo APEX:** arrastrarlo en el inventario justo después de cogerlo rompía la interfaz. Ahora se absorbe al cerrar el inventario.
- **Perks como inyectores:** el botón "Usar" salía en gris. Arreglado por Kaganari (PR #1, ¡gracias!).
- **Daga de puño Akula** (y manos de infectado): ya no usan los estilos de cuerpo a cuerpo; el arma ya no flota en el aire.
- **Perks Eternas y de Classic:** al empezar la raid se comprueba que sigan activas y se reaplica Deadshot en el arma.

#### Novedades de la 3.0
La actualización más grande hasta ahora: 3 perks nuevas, máquinas de perks en el mapa, la Caja Misteriosa, TEDD, APEX, dos armas de Black Ops 7 y perks para enemigos y compañeros. Los modelos de Black Ops 6/7 vienen solo compilados, para jugar.

##### Lo nuevo

###### 3 perks nuevas
| Perk | Efecto base | Cómo conseguirla |
|---|---|---|
| **Bandolier Bandit** | Los cargadores del chaleco y los bolsillos se rellenan 1 bala cada 2 s con la munición suelta que llevas, mientras no disparas. | Therapist (7 GP), botín en raid y la Wunderfizz. |
| **Vulture Aid Elixir** | Los contenedores y cuerpos sin registrar con objetos de valor brillan a través de las paredes (15 m). | Igual que Bandolier. |
| **Melee Macchiato** | Daño cuerpo a cuerpo ×2 y golpes un 30% más rápidos. | Igual que Bandolier. |

Cada una tiene su árbol completo en **Research**, con mejoras, desventajas, desafíos y su canción. Vulture Aid tiene la mejora **Re-roll**: mirando un contenedor de cerca, pulsa **J** (configurable en F12) y su botín se vuelve a tirar.

###### Máquinas de perks en el mapa (por ahora en Factory, de día y de noche)
- Las máquinas de Perk-a-Cola están **en el mapa**. Mantén **F** delante de una para comprar su perk por **30.000 ₽**. Si ya la tienes, la vuelves **Eterna** por **15.000 ₽**.
- **Hay que encender la luz:** las máquinas no funcionan hasta que activas el interruptor del mapa. Se puede desactivar en F12.
- **Wunderfizz:** una perk al azar por **40.000 ₽**, con su ruleta.

###### Caja Misteriosa
- Aparece en **uno** de sus puntos del mapa en cada raid. Cuesta **1.000 ₽**.
- **Mantén F** para un **arma al azar** (montada, con su preset). **Mantén Y** (configurable en F12) para un **accesorio al azar**.
- Como en Zombies: la tapa se abre, los modelos van pasando y el premio queda flotando. Cógelo con la F de EFT antes de que se hunda.
- Tras varios usos en el mismo sitio puede salir el **osito**: te devuelve el dinero y la caja se va volando a otro punto del mapa.
- Tiene un 15% de dar una de las armas nuevas de Black Ops 7.

###### TEDD
- Aparece en **algunas raids** (15% por defecto, configurable en F12), en uno de sus puntos. Brilla en verde y se marca en pantalla cuando estás cerca.
- Háblale (**mantén F**) y te da **una prueba al azar**, con 5 minutos de tiempo:
  - **Objeto:** aparece un objeto lejos y tienes que llevárselo.
  - **Abatir:** aparece un PMC marcado en pantalla y tiene que morir.
  - **Aguantar:** 90 s cerca de TEDD mientras llegan oleadas de infectados.
  - **Dinero:** págale la cantidad que pide.
- **Recompensa:** una botella de perk al azar.

###### APEX
Un efecto **muy fuerte** para cada perk. Solo puedes llevar **uno** equipado (en Modern y en Classic) y funciona con su perk activa.
1. Consigue un **Núcleo APEX**: aparece raramente en cajas fuertes, alijos, cajas de armas y cuerpos de PMC. También lo vende Mechanic (nivel 1) por 5.000.000 ₽, y sale en el buscador del mercadillo. Para que cuente, cógelo o llévalo **encima en una raid** y **extrae con vida**.
2. En **Research > APEX**, gasta un núcleo para empezar el desafío de un APEX. Solo avanza con su perk activa.
3. Supera el desafío y equípalo.

| APEX | Efecto |
|---|---|
| Juggernog | Inmune al cuerpo a cuerpo y sin sangrados |
| Speed Cola | Tu arma no se encasquilla |
| Stamin-Up | Estamina infinita y +10% de velocidad al esprintar |
| PHD Flopper | Cada baja provoca una explosión (a ti no te hace daño) |
| Widow's Wine | Tus explosivos detonan solos al pasar un enemigo cerca (3 m) |
| Deadshot | +10 de penetración (una clase de blindaje más) |
| Bandolier | Al hacer una baja se rellena tu cargador |
| Vulture Aid | El 50% de los enemigos llevan un objeto valioso o de misión |
| Melee Macchiato | No recibes daño mientras das un golpe |

###### Armas de Black Ops 7
- **MK35 ISR** (fusil de asalto, sobre la HK416) y **VMP** (subfusil, sobre la MP5).
- Tienen modelo propio, miras de hierro utilizables y admiten **los accesorios de su arma base** y camuflajes.
- **Cómo conseguirlas:** Mechanic (nivel 1), ya montadas, o en la Caja Misteriosa.

###### Perks en enemigos y compañeros
- **Enemigos:** los que llevan una perk encima **tienen su efecto**. Juggernog aguanta el doble, Speed Cola dispara más rápido, Deadshot sabe dónde estás aunque haya paredes, Widow's Wine te frena al acertarte, etc. Los que llevan Bandolier o Vulture Aid aparecen con munición extra o con un objeto valioso.
- **Compañeros de PIT Fireteam:** al abrir su inventario para darle o cambiarle equipo, aparece abajo un panel con **4 casillas de perks**.
  - Pulsa **+** y elige una perk que lleves encima: se le equipa y la botella se gasta al cerrar el inventario.
  - Pulsa una perk equipada para quitársela: la botella vuelve a tu equipo.
  - Las perks se quedan entre raids hasta que el compañero **muere**.
- Se puede desactivar todo en F12: *Efectos de perks en enemigos y compañeros*.

###### Otros
- **Perks como inyectores** (F12, desactivado por defecto): las perks pasan a ser inyectores con su animación, en vez de latas. Hay que reiniciar el servidor y el juego.
- **Estilos de cuerpo a cuerpo:** combos según el arma. Las ligeras hacen horizontal, vertical y estocada; las medias, horizontal, revés y vertical; las pesadas, un golpe con peso. Los cuchillos se ven más al centro de la pantalla.
- **Interfaz renovada** en Research y en el panel de la raid: fondo animado, burbujas, esquinas, brillos al pasar el ratón y un color que sigue a la perk elegida.
- **Icono nuevo** del mod.
- **Opciones de desarrollo** (grabar clips, depuración) ocultas en F12. Se ven activando *Advanced settings*.


##### Lo que cambió
- **Revisión de desventajas:** las desventajas que eran castigos genéricos (hambre, sed, correr, sacar el arma) ahora castigan en el mismo terreno en que ayuda su perk. Lo que tenías equipado sigue equipado; solo cambia el efecto. PHD Flopper gana la mejora **Caída explosiva** y la desventaja **Adicto a la pólvora**.
- **Animación de beber de Black Ops:** queda **desactivada** y pasa a F12 > Debug como experimental. Se mezclaba con la de EFT (manos negras y lata flotando), así que ahora solo se ve la de EFT.
- Las perks de los compañeros de PIT ya no se leen de su mochila: se equipan en sus 4 casillas.


##### Pendiente (próximas versiones)
- **Máquinas, caja y TEDD en todos los mapas.** Los mapas sin máquinas puestas a mano las reciben solas (junto a paredes, cerca del botín; F12 > Máquinas > *Colocación automática*: Fixed / EveryRaid / Off). Pon las tuyas donde quieras con el **editor de máquinas** (F12 > Máquinas, o **Fin** en raid): eliges la pieza (máquinas de perk, Der Wunderfizz, punto de la caja misteriosa, punto de TEDD, interruptor de la luz) con RePág/AvPág, la giras con [ ], la colocas con Insert y borras la más cercana con Supr. Lo puesto a mano manda sobre lo automático.
- Misiones nuevas: APEX (3), Caja Misteriosa (1) y TEDD (2).
- Arreglar la animación de beber de Black Ops.
- Sonidos de la Caja Misteriosa.
- Master of Movement (deslizamiento de PHD) y pestaña de estadísticas.

#### Hotfix 2.2.1
- Compatibilidad con mods que restauran el equipo al morir (p. ej. Fair Equipment Restoration): las devoluciones de botellas (Eternas / Clásico / Widow's Bite) y las recompensas de logros se envían al terminar la raid, nunca durante.
- Los logros conseguidos al morir se procesan bien.

#### Novedades de la 2.2
- **Arreglo (importante):** al llegar a nivel 5 en una perk podía salir un error en blanco al empezar la raid y romper el inicio de raid de otros mods (HUDs, contadores de botín). El inicio de la raid ya está aislado de cualquier error y los logros se revisan cuando tu personaje ya existe.
- **Compatibilidad con Arena Mode:** su modo Zombies da una perk aleatoria cada 5 oleadas y las perks ganan progreso lento. Las partidas de Arena nunca cuentan para tu perfil de SPT (se conservan las perks de Clásico y las Eternas). Opciones en F12 > Arena.
- **Muchas más perks en el mundo:** botiquines, cajas de armas, bolsas de deporte, Scavs muertos, alijos, cajones y chaquetas, y **sueltas** donde pueden salir bebidas. Más probabilidad en todo (PMC 10%, Scavs 3%, Rogues 12%). Tu `loot.json` viejo se reemplaza (copia en `loot.json.old`).
- **Revisión de mejoras:** se rehicieron mejoras que anulaban su propia desventaja, desafíos repetidos y desventajas que no se acumulaban (Deadshot, Juggernog, Speed Cola, Stamin-Up, PHD Flopper, Widow's Wine). Los desafíos cambiados reinician su progreso si no estaban completos.
- **Dead Aim** ahora deja el arma fija en el centro: sin balanceo al moverte, al girar ni al apuntar (el retroceso se mantiene).
- **Imágenes** para las 10 misiones de la historia (faltaban).
- ???

#### Novedades de la 2.1
- **Menú F12 en inglés** (o español): los nombres de las opciones siguen el idioma del mod (Auto = idioma del juego). Tu configuración se conserva.
- **Opción para ocultar los iconos de perks** de abajo de la pantalla (F12 > HUD > Mostrar iconos de perks).
- **Aviso de enemigos con perks:** un aviso cuando te acercas a un enemigo que lleva perks (distancia en F12).
- **Las perks aparecen en más sitios:** cajas de madera, cajas de munición, cajas de granadas (Widow's Wine / PHD Flopper), cajas de raciones (Juggernog), cuerpos de PMC y cajas fuertes (casi siempre). **Los jefes siempre llevan 2 perks**; algunos PMC, Rogues y (rara vez) Scavs llevan una. Las probabilidades están en `user/mods/TarkovaCola/loot.json`.
- **Arreglo:** las perks se añadían a contenedores equivocados (Scavs muertos y chaquetas en vez de cajas de munición y cajas fuertes).
- Los objetos de misión ahora tienen su aspecto (cuaderno, sobre, muestra...).

</details>

---

## English

### Hotfix 4.0.1
- **Maps Expanded** support: perk machines, Der Wunderfizz, the Mystery Box, TEDD, the power switch and Death Perception marks also show on its map. Contributed by **mattpsvreis** (PR #2, thanks!).
- Textures capped at 1024 so the download is smaller (593 MB instead of 1.3 GB).

### What's new in 4.0: The Last Round
This is the last big update of Tarkova-Cola, and the biggest by far. It adds **6 new perks** (15 in total), **6 more Black Ops 7 weapons** (8 in total), Black Ops drinking and melee animations, machines on **every map**, a perk case, 45 new achievements, a balance pass on every perk and compatibility with many popular mods. All the texts can now also be **translated**.

> **Updating from 3.x:** extract the zip over your SPT folder and overwrite. Your progress, Eternal perks and Classic perks are kept.

#### At a glance
| | |
|---|---|
| **New perks** | Quick Revive, Mule Kick, Electric Cherry, Death Perception, Tombstone Soda and Double Tap Root Beer, each with its full Research tree, APEX, jingle, bottle and injector |
| **Black Ops 7 weapons** | Peacekeeper Mk1, Voyak KT-3, MXR-17, VST, M10 Breacher and Hawker HX, with BO7 animations and sounds. MK35 ISR and VMP also get them |
| **Animations** | Black Ops 7 drinking animation for every perk, and Black Ops 7 melee animations for all 23 EFT melee weapons |
| **Machines** | Machines, Mystery Box, TEDD and the power switch on **every map**, placed automatically. A machine editor lets you place your own |
| **Mystery Box** | Can give the Ray Gun, Ray Gun Mark II and Wunderwaffe DG-2 when those mods are installed |
| **Perk case** | A case that only holds perks and APEX cores |
| **Achievements** | 45 new, 127 in total |
| **Classic** | The modifier perk now gives 50% of its effect plus a fixed signature augment |
| **Compatibility** | SAM (Advanced Movement), Dynamic Maps, Maps Expanded, SAIN, QuestingBots, K9 Companion, PIT Fireteam, TacticalReload, Weapon Camo and Stickers, and Kenner's wonder weapons |
| **Translations** | Every text can be translated with JSON files (see `TRANSLATING.md`) |

---

#### The 6 new perks
| Perk | Base effect |
|---|---|
| **Quick Revive** | After 8 s without taking damage, you regenerate 1 HP per second in one injured body part at a time (head and thorax first), even while sprinting. The health is drawn from one of your medkits (never the one in your hands): no medkit, no regeneration. |
| **Mule Kick** | A **3rd weapon slot** that takes any primary. Reach it with the mouse wheel. Swapping to or from it is 50% faster. |
| **Electric Cherry** | When a reload finishes you get an electric **sprint speed boost** for 1 s. The emptier the magazine was, the bigger the boost (up to +150%). Works with every weapon: magazines, shotgun tubes, revolvers and bolt-actions. |
| **Death Perception** | You see better in the dark, and enemies you see within 20 m get an **orange outline** until you lose sight of them. |
| **Tombstone Soda** | When a hit leaves you below 25% of your total health, you drop a **decoy tombstone** and become a **specter** for 4 s. Bots lose you and shoot the tombstone, and you move 30% faster but can't shoot. At most once every 3 minutes. |
| **Double Tap Root Beer** | Fire rate ×1.10, and every consecutive hit within 0.4 s raises it by another 3% (up to +15%). Missing or stopping resets the rhythm. |

**How to get them:** Therapist (loyalty level 1, 7 GP Coins each), raid loot (the same places as the other perks), the **Der Wunderfizz** machine, TEDD's trials and the random perk drop. They are not locked behind the storyline.

Like every perk, each one levels 1 → 5, reveals challenges and unlocks **3 major augments, 4 minor augments and an Extra Slot**, each with its drawback (3 major, 4 minor).

##### Quick Revive
| Type | Augment | Effect |
|---|---|---|
| Major | **Revive** | Needs a stimulant in your inventory (it uses one). A lethal hit leaves you **Downed** for 10 s at 1 HP: your weapon is taken, you only have a pistol and you crawl. Kill an enemy to get back up with 30% health and keep your perks; otherwise you die. Once per raid. |
| Major | **Transfusion** | The regeneration restores destroyed body parts (back to 1 HP), but gives them no more health. From there only medicine heals them. |
| Major | **Combat Medic** | Taking damage no longer stops the regeneration: in combat you heal 0.3 HP/s. |
| Minor | **Patience** | Regeneration starts after 5 s without damage (instead of 8). |
| Minor | **Priority** | Head and thorax regenerate 50% faster. |
| Minor | **Clotting** | While you regenerate, light bleeds stop. |
| Minor | **Reserve** | When you take damage, you heal 2 HP/s for 3 s; then it stops until the normal wait is over. |
| Major drawback | Thin Blood · Costly Metabolism · Startle | Waits 14 s · each HP costs 0.5 energy and 0.5 hydration · your own shots reset the wait |
| Minor drawback | Lukewarm Regen · Scars · No Rush · Weak Mending | −25% healing · heals only up to 80% · stops while sprinting · +10% damage taken while regenerating |
| APEX | | Revive recharges every 7 PMC kills (it still needs a stimulant). |

##### Mule Kick
| Type | Augment | Effect |
|---|---|---|
| Major | **Mule's Kick** | The first shot within 2 s of swapping weapons deals 50% more damage. |
| Major | **Mule Bandolier** | The Mule Kick weapon reloads itself while stowed (1 round every 1.5 s, from the loose ammo you carry). |
| Major | **Quick Change** | Swapping weapons with an empty magazine is instant. |
| Minor | **Light Strap** | The Mule Kick weapon weighs half. |
| Minor | **Chamber Ready** | The Mule Kick weapon always comes out chambered. |
| Minor | **Balance** | For 2 s after drawing the Mule Kick weapon, sway drops by 15%. |
| Minor | **Holster** | Swapping between your other weapons is 30% faster too. |
| Major drawback | Mule Load · Loose Strap · Clumsy Swap | the weapon weighs 50% more · a hit of 30+ damage has a 5% chance to drop it · swapping to or from it is 30% slower |
| Minor drawback | Empty Chamber · Safety On · Clatter · Tangled | comes out unchambered · first shot delayed 0.3 s · drawing it makes noise (15 m) · can't reload it while sprinting |
| APEX | | When you swap weapons, the one you draw gets a chambered round and 5 rounds in its magazine. |

- The slot only accepts weapons while the perk is active. If the perk ends with a weapon still in it, the slot stays visible so you can take it out.
- The mouse wheel can be turned off in F12 (*Mule Kick: mouse wheel swaps weapons*).
- Works alongside DualWieldLab's extra holster.

##### Electric Cherry
| Type | Augment | Effect |
|---|---|---|
| Major | **Discharge** | Reloading also releases an electric burst around you: it stuns enemies for up to 1.5 s within 2-5 m, depending on how empty the magazine was. |
| Major | **Overcharge** | Reloading during the boost stacks it (up to +225%) and renews its duration. |
| Major | **Lightning** | During the boost, sprinting uses no stamina and you can reload while sprinting. |
| Minor | **Battery** | The boost lasts 2 s (instead of 1). |
| Minor | **Voltage** | Any reload gives at least +45% speed. |
| Minor | **Feedback** | A kill during the boost brings it back to its starting value. |
| Minor | **Steady Current** | The boost doesn't fade: it stays at full and cuts out at the end. |
| Major drawback | Leak · Polarity · Backlash | lasts 0.5 s · only reloads with the magazine at least half empty count · 15% slower for 1.5 s when it ends |
| Minor drawback | Short Circuit · Visible Spark · Tingling · Low Ceiling | taking damage cancels it · bots hear you crackle (20 m) · +15% sway · max boost +105% |
| APEX | | Every reload gives the maximum boost and releases the electric burst. |

##### Death Perception
| Type | Augment | Effect |
|---|---|---|
| Major | **Sixth Sense** | You see through walls: enemies in range are marked even when you can't see them. |
| Major | **Hunter's Eye** | Marks have no distance limit (instead of 20 m). |
| Major | **Killer Instinct** | Enemies are no longer marked. Instead, a warning shows when an enemy looks at you from behind, and every kill leaves a **UAV pulse** on the body for 30 s that reveals enemies within 15 m of it through walls. |
| Minor | **Persistence** | The mark stays 3 s after you lose sight of the enemy. |
| Minor | **Cat's Eyes** | Stronger dark vision, and flashbangs blind you half as much. |
| Minor | **Shrapnel Sense** | Enemy grenades are marked with their trajectory and danger radius. |
| Minor | **Reading** | The outline's color shows the enemy's health. |
| Major drawback | Interference · Short Sight · Paranoia | outlines flicker · marks reach only 12 m · fake marks appear now and then |
| Minor drawback | Lag · Glare · Migraine · Overload | marks update once a second · flashbangs blind you 30% more · each mark costs 2 energy · +15% sway with 3+ marks |
| APEX | | Marks stay 5 s without line of sight and show the enemy's health. |

- The outline is **edge only**, thin and translucent, so it doesn't cover the enemy. Its opacity is in F12 (*Outline opacity*).
- Dark vision raises brightness when the scene is dark, without a night-vision tint.

##### Tombstone Soda
| Type | Augment | Effect |
|---|---|---|
| Major | **Offering** | If you die, what you carry in your pockets and rig comes back by mail (once per raid). |
| Major | **Poltergeist** | The tombstone bursts when the specter ends and stuns enemies within 5 m for 2 s. |
| Major | **Haunting** | You can shoot as a specter: the first shot deals double damage and ends the specter. |
| Minor | **Long Haunt** | The specter lasts 6 s (instead of 4). |
| Minor | **Restless** | Tombstone recharges in 2 minutes (instead of 3). |
| Minor | **Ectoplasm** | Becoming a specter stops bleeds and heals 15 HP. |
| Minor | **Cold Trail** | When the specter ends, bots take 3 s longer to find you. |
| Major drawback | Short Haunt · Heavy Soul · Marked | lasts 2 s · recharges in 5 minutes · the closest bot knows where you are when it ends |
| Minor drawback | Cold · Wail · Slow Ghost · Fragile Tombstone | no stamina when it ends · a 40 m noise · no speed bonus · the tombstone draws fire for only 1.5 s |
| APEX | | Every PMC kill recharges Tombstone, and the specter lasts 6 s. |

##### Double Tap Root Beer
| Type | Augment | Effect |
|---|---|---|
| Major | **Double Shot** | At max rhythm, each hit has a 15% chance to count as two bullets (×2 damage) without using an extra round. |
| Major | **Perforation** | The rhythm also raises penetration (up to +8 at max rhythm), only against armor of class 4 or lower. Great for SMGs and pistols. |
| Major | **Momentum** | Missing no longer resets the rhythm; only stopping fire for 1 s does. |
| Minor | **Light Trigger** | Semi-automatic weapons also build rhythm with every hit. |
| Minor | **Cold Barrel** | The extra fire rate doesn't heat up your weapon. |
| Minor | **Feed** | The extra fire rate doesn't raise malfunctions. |
| Minor | **Metronome** | The rhythm goes up to +25% (instead of +15%). |
| Major drawback | Hungry Mag · Out of Step · Jam | at max rhythm 5% of shots use 2 rounds · missing slows you 10% for 1 s · +50% malfunctions at max rhythm |
| Minor drawback | Slow Start · Kick · Wear · Irregular | first 3 shots 10% slower · +10% recoil at max rhythm · +20% weapon wear · the bonus varies ±10% |
| APEX | | The rhythm always starts at max. |

---

#### Perks on enemies and teammates
Enemies carrying a perk get its effect, and PIT Fireteam teammates get the perks you equip in their 4 slots. A notice warns you when an enemy perk triggers, and the icons of their perks float over their heads (in line of sight, within 60 m; F12 > *Perk icons over enemies*).

| Perk | On an enemy |
|---|---|
| Juggernog | Takes half damage |
| Speed Cola | Reloads ×1.5 faster |
| Stamin-Up | Infinite stamina and immune to pain |
| PHD Flopper | Immune to explosions and falls |
| Widow's Wine | Its shots slow you; its grenades web you (8 m) |
| Deadshot | Knows where its enemies are through walls (80 m) and +30% penetration |
| Bandolier Bandit | Spawns with extra ammo |
| Vulture Aid | Spawns with a valuable item |
| Melee Macchiato | Melee ×2. Takes 80% less damage until you hit it in melee, and a head or thorax shot won't drop it before the rest of its body |
| **Quick Revive** | Regenerates. A lethal hit can leave it **downed** instead of dead: finish it with a headshot, or it gets back up later with full health and comes for you |
| **Mule Kick** | Spawns with a 3rd weapon, and swaps weapons instead of reloading |
| **Electric Cherry** | Reloading releases a discharge (3 m) that hurts and stuns you |
| **Death Perception** | Hunts only you and roughly knows where you are |
| **Tombstone Soda** | Drops a live grenade when it dies (3 s to get away) |
| **Double Tap** | Fires 25% faster |

Everything can be turned off in F12 > *Perk effects on enemies and teammates*.

---

#### Black Ops 7 weapons
Six new weapons, ported with their **own models, first-person animations and sounds** from Black Ops 7, built on an EFT weapon so they take its attachments and camos. **MK35 ISR** and **VMP** now also have their BO7 animations and sounds.

| Weapon | Type | Built on | Price |
|---|---|---|---|
| **MK35 ISR** | Assault rifle | HK416A5 (5.56) | |
| **VMP** | SMG | MP5 Navy (9×19) | |
| **Peacekeeper Mk1** | Assault rifle | AK-12 (5.45) | 120,000 ₽ |
| **Voyak KT-3** | Assault rifle | AK-74M (5.45) | 105,000 ₽ |
| **MXR-17** | Assault rifle | SCAR-L (5.56) | 115,000 ₽ |
| **VST** | SMG | MP9 (9×19) | 85,000 ₽ |
| **M10 Breacher** | Pump shotgun | M590 (12ga) | |
| **Hawker HX** | Bolt-action sniper | AXMC (.338 LM), with a S&B scope | |

- **How to get them:** Mechanic (loyalty level 1), already built. MK35 ISR and VMP also come out of the Mystery Box.
- Usable iron sights, correct icons and the same size as their real counterparts.
- Compatible with **Weapon Camo and Stickers**.

#### Black Ops animations
- **Drinking:** every perk is drunk with the Black Ops 7 animation (the can in your hand, the pull tab, the gulp). On by default; F12 > *Black Ops drinking animation*. The can size in your hand is also in F12.
- **Melee:** all 23 EFT melee weapons use Black Ops 7 animations (knives, machetes, axes, crowbars, sledgehammers...). F12 > *Black Ops melee animations*; turn it off to go back to the melee styles.

---

#### Machines on every map
- Perk machines, Der Wunderfizz, the Mystery Box spots, TEDD's spots and the **power switch** now appear on **every map**. Maps without hand-placed spots get them **automatically**: next to walls, close to loot, on the navmesh, spread out.
  - F12 > Machines > *Automatic machine placement*: **Fixed** (found once and kept), **Every raid** (new spots every raid) or **Off**.
  - F12 > Machines > *Redo automatic machines*: the next raid throws away that map's automatic spots and finds new ones.
- **Machine editor** (F12 > Machines, or **End** in raid): place your own pieces anywhere. Hand-placed pieces always win over automatic ones.

  | Key | Action |
  |---|---|
  | PageUp / PageDown | Next / previous piece (perk machines, Der Wunderfizz, Mystery Box spot, TEDD spot, power switch) |
  | [ / ] | Rotate |
  | \\ | Snap the rotation to 45° |
  | Insert | Place |
  | Delete | Delete the nearest piece |
  | F11 | Screenshot |

- **Der Wunderfizz** is the machine for the 9 perks without their own machine (Bandolier, Vulture Aid, Melee Macchiato and the 6 new ones).
- Machines are easier to use: 1.6 m reach and more tolerance for height differences.
- **Dynamic Maps** or **Maps Expanded** shows the machines and the power switch on its map (see Compatibility). Whichever of the two is installed is used; neither is required.

#### Mystery Box
- **Wonder weapons:** with Kenner's **Ray Gun**, **Ray Gun Mark II** or **Wunderwaffe DG-2** installed, the box has a 10% chance to give one of them, loaded the same way their wall buy hands them out.
- Black Ops 7 weapons: 15%.

#### Perk case
- A case (2×2 outside, 5×4 inside) that only holds perk bottles, perk injectors and APEX cores.
- Therapist (loyalty level 1, 65,000 ₽) and the flea market.

#### APEX
- The 6 new perks have their APEX (see each perk above).
- **APEX Stamin-Up:** stamina regenerates ×3 (it used to be infinite).
- **APEX Vulture Aid:** 15% of enemies (it used to be 50%), and never LEDX, graphics cards or Bitcoin.
- **APEX Bandolier:** its challenge needs 40 kills (it used to be 60).
- **APEX cores** stop being absorbed once there are no challenges left to start, so you can sell them.

#### Classic mode
- The 5th perk (the **modifier**) now gives **50%** of its base effect, plus its fixed **signature minor augment**:

  | Perk | Signature | Perk | Signature |
  |---|---|---|---|
  | Speed Cola | Mag Juggler | Bandolier Bandit | Cold Barrel |
  | Stamin-Up | Light Feet | Vulture Aid | Sharp Eyes |
  | Juggernog | Thick Skin | Melee Macchiato | Quick Draw |
  | PHD Flopper | Soft Soles | Quick Revive | Patience |
  | Widow's Wine | Quick Spinner | Mule Kick | Chamber Ready |
  | Deadshot | Quick Scope | Electric Cherry | Battery |
  | Death Perception | Persistence | Tombstone Soda | Long Haunt |
  | Double Tap | Cold Barrel | | |

- The modifier can also earn a 2nd minor augment through the raid challenge.

#### Achievements
- **45 new achievements**, 5 for each perk added since 3.0 (Bandolier, Vulture Aid, Melee Macchiato and the 6 new ones): first drink, total progress, level 5, all challenges and a secret one. **127** in total.
- New sound when you complete a challenge: the Black Ops 3 rank-up.

---

#### Compatibility with other mods
Everything is optional and detected automatically: without these mods nothing changes.

| Mod | What Tarkova-Cola does with it |
|---|---|
| **SAM (Sage's Advanced Movement)** | With **PHD Flopper**, landing a dive sets off an explosion where you land (the classic dolphin dive; it never hurts you or your armor). The slide is **twice as fast** and goes 50% farther, with no slow tail, pink energy trail, sparks and sound. Crouching again ends it instantly, and you come out of it sprinting at full speed. |
| **Dynamic Maps** / **Maps Expanded** | Optional. If either mod is installed, its map and minimap show the perk machines (with their perk icon), Der Wunderfizz, the Mystery Box, TEDD and the power switch, and the enemies marked by **Death Perception**, live. The markers are added at runtime; Tarkova-Cola still loads if neither map mod is present. The two map mods cannot be installed together. |
| **SAIN** | Stunned bots (Widow's Wine, Electric Cherry, Tombstone) stop shooting while the stun lasts. |
| **QuestingBots** | The storyline guards and bosses stay at their post instead of going off on quests. |
| **K9 Companion** | Your dog takes no damage from perk explosions (PHD dive, APEX). |
| **PIT Fireteam** | Teammates use the perks in their 4 slots, and perks never stun, mark or hurt them. |
| **Allies in general** | Any bot that isn't hostile to you (for example Scavs when you play as a Scav) is never stunned, marked or hurt by your perks. If it attacks you, it stops counting as an ally. |
| **Ray Gun, Ray Gun Mark II, Wunderwaffe DG-2** (Kenner) | They can come out of the Mystery Box. |
| **TacticalReload** | Speed Cola's 2 shells per insertion respect the tube capacity together with its multi-shell reloads (Benelli M3). |
| **MagRetention, Kirisame Mag Reload, StripperClips, Speedloader** | Speed Cola and Electric Cherry work with their reloads. |
| **Weapon Camo and Stickers** | Camos and stickers apply to the Black Ops 7 weapons. |
| **DualWieldLab** | The Mule Kick slot works alongside its extra holster. |
| **Arena Mode** | Supported since 2.2 (Zombies mode drops perks). |

#### Translations
- **Every text** in the mod can be translated with plain JSON files: the Research, APEX and Classic screens, notices, perks, augments, machines, F12 options, items, achievements and quests.
- The mod writes up-to-date **templates** (`en.json` and `es.json`) every time it starts, in `BepInEx/plugins/TarkovaCola/locales/` and `SPT_Runtime/user/mods/TarkovaCola/locales/`.
- Copy `en.json` as your language code (`ch`, `ru`, `fr`, `de`, `jp`...), translate the values and restart. F12 > *Language* lists every installed translation.
- The full guide is in **`TRANSLATING.md`**. Send your translation and it will be bundled with credit to you.

#### Other changes
- **Speed Cola on manual weapons:** pump shotguns, bolt-action rifles and revolvers reload **twice as fast** and cycle the bolt or pump twice as fast. Tube-fed shotguns load **2 shells per insertion**.
- **PHD Flopper:** explosions no longer wear your armor.
- **F12 > Audio:** separate volume for the perk jingles and for the sound effects.
- **F12 > Perks > Pause perk effects:** turns every perk effect off without losing your perks, to compare.
- **Bandolier and Widow's Wine** have their own cans, and the can icons in the inventory are no longer white.
- Perks can't be drunk in the Hideout.
- The **Research**, APEX, Eternal and ESC panels fit the 15 perks.

#### Balance pass
**Challenges that could be cheated**
- PHD Flopper's absorb challenge only counts falls that would really hurt.
- Vulture Aid's search challenge needs you to open the container or body.
- Several challenges now need their perk active.

**Lower goals**

| Augment | New challenge |
|---|---|
| Stamin-Up: Rush Hour | 3 kills in under 3 minutes, 3 times |
| Widow's Wine: Nest | Web 3 enemies with one grenade, 5 times |
| Melee Macchiato: Flash Strike | 100 melee attacks |
| Melee Macchiato: Charge | Sprint 1 km with a melee weapon |
| Deadshot: To the Skies | Fire 800 rounds |
| Bandolier: Cold Barrel | Fire 600 rounds |
| Juggernog: Bone Plating | 40 hits on your armor |
| Juggernog: Skull Cap | Take 600 damage |
| Stamin-Up: Light Feet · PHD: Terrain Walker | 3 km |

**Effects**
- **Dead Aim:** sway ×0.35 instead of none.
- **Second Wind:** 90 s cooldown.
- **Iron Skull:** once per raid.
- **Web radius:** 8 m.

**Drawbacks that now affect their own perk**

| Perk | Drawback | Effect |
|---|---|---|
| Juggernog | **Cold Armor** | The damage reduction starts at 0 on the first hit of a fight and takes 3 s to reach full. |
| PHD Flopper | **Own Wave** | Explosions don't hurt you, but they shake you: +30% sway for 2 s. |
| Deadshot | **Tired Eye** | After aiming for 8 s straight, the sway reduction drops by half. |
| Vulture Aid | **Picky Vulture** | Only containers and bodies worth 100,000 ₽ or more are marked. |
| Melee Macchiato | **Fleeting Edge** | The ×2 damage only applies to the first hit on each enemy. |
| Speed Cola | **Noisy Reload** | Every reload with Speed Cola makes noise that bots within 15 m hear. |

**Removed**
- **Synergies.** Their achievements are hidden; your other achievements are untouched.
- The developer tools in F12 (copy position, record clip, melee debug), and the *Melee styles* and *Center light melee* options: the styles are always on when the Black Ops melee animations are off.

---

### What it is
Drinks you find, buy or craft that grant an in-raid ability. The more you use them, the more the perk levels up: challenges are revealed and completing them unlocks **augments** (and their linked **drawbacks**) that you equip in the **Research** screen.

### The perks

| Perk | Base effect (rest of the raid) | Machine |
|---|---|---|
| **Speed Cola** | Reloads ×1.5 faster (×2 and 2 shells per insertion on manual weapons) | Own |
| **Stamin-Up** | Leg stamina ×1.5 | Own |
| **Juggernog** | −33% damage taken | Own |
| **PHD Flopper** | Immune to fall and explosion damage, and explosions don't wear your armor | Own |
| **Widow's Wine** | Your grenades web enemies (−60% movement speed) | Own |
| **Deadshot Daiquiri** | Aim 15% faster and 25% less weapon sway | Own |
| **Bandolier Bandit** | Magazines in your rig and pockets refill from loose ammo you carry | Der Wunderfizz |
| **Vulture Aid Elixir** | Unsearched containers and bodies with valuables glow through walls (15 m) | Der Wunderfizz |
| **Melee Macchiato** | Melee damage ×2 and melee attacks 30% faster | Der Wunderfizz |
| **Quick Revive** | Regenerate 1 HP/s after 8 s without damage, drawn from your medkits | Der Wunderfizz |
| **Mule Kick** | A 3rd weapon slot, and faster swaps to and from it | Der Wunderfizz |
| **Electric Cherry** | A sprint speed boost after every reload (up to +150%) | Der Wunderfizz |
| **Death Perception** | Dark vision and an outline on the enemies you see (20 m) | Der Wunderfizz |
| **Tombstone Soda** | Below 25% health, a decoy tombstone and 4 s as a specter | Der Wunderfizz |
| **Double Tap Root Beer** | Fire rate ×1.10, up to +15% more with consecutive hits | Der Wunderfizz |

Each perk levels 1 → 5 with the XP of the raids you carry it in (+25%). Each level reveals challenges; each challenge unlocks an augment and its drawback. You equip 1 major + 1 minor augment (2 with Extra Slot) and **every augment needs its drawback equipped** (major with major, minor with minor).

**Where to get perks:** Therapist (GP Coins), raid loot (safes, PMC bodies, medical cases, weapon boxes, loose drink spots...), the hideout Nutrition Unit (recipes from the storyline), the machines on every map, TEDD, the random perk drop and enemies carrying them.

### Modes (switch in Research)
- **Modern**: full Research, as many perks as you like.
  - **Eternal Perk**: drink a perk you already have and it becomes **eternal** (blue outline): it stays with you between raids, loses one drawback and, if you die, its bottle comes back by mail. **ETERNAL** tab in Research.
- **Classic**: no Research, no drawbacks. Up to 4 perks + 1 **modifier** (50% of its effect plus its signature augment). Each perk rolls 1 random minor augment and **they stay with you until you die**. Every raid brings an optional challenge that grants a 2nd augment.
- **APEX** (both modes): one very strong effect per perk, unlocked with APEX Cores and challenges.

| APEX | Effect |
|---|---|
| Juggernog | Immune to melee and no bleeding |
| Speed Cola | Your weapon never jams |
| Stamin-Up | Stamina regenerates ×3 and +10% sprint speed |
| PHD Flopper | Every kill sets off an explosion (it doesn't hurt you) |
| Widow's Wine | Your explosives go off on their own when an enemy walks close (3 m) |
| Deadshot | +10 penetration (one armor class higher) |
| Bandolier | A kill refills your magazine |
| Vulture Aid | 15% of enemies carry a valuable or quest item |
| Melee Macchiato | You take no damage while swinging |
| Quick Revive | Revive recharges every 7 PMC kills |
| Mule Kick | The weapon you draw gets a chambered round and 5 rounds |
| Electric Cherry | Every reload gives the maximum boost and the electric burst |
| Death Perception | Marks stay 5 s without line of sight and show health |
| Tombstone Soda | PMC kills recharge it, and the specter lasts 6 s |
| Double Tap | The rhythm always starts at max |

### On the map
- **Perk machines** (30,000 ₽, or 15,000 ₽ to make a perk you have Eternal), **Der Wunderfizz** (random perk, 40,000 ₽), the **Mystery Box** (1,000 ₽: hold F for a weapon, Y for an attachment) and **TEDD** (random trials that reward a perk), on every map. Turn on the **power switch** first.
- **Random perk:** kills have a 4% chance to drop a **floating bottle** (at most one per raid, 60 s). Grab it: a random perk, or an eternal one if you already had it. With announcer voice.

### Storyline: the Bottlers (requires VisitAPI)
10 quests from **Skier** and **Therapist** (from level 12):
- **3 intro quests**: unlock the perk **recipes** in the hideout **Nutrition Unit**.
- **6, one per original perk**: unlock that perk's **sale** at Therapist. The guards really carry the quest's perk (with its icon over their heads).
- **Final**: **the Bartender**, with every perk (4 in Classic mode).

With VisitAPI installed, **the 6 original perks are not sold by Therapist until you finish their quest** (they can still be found in raid and crafted). The 9 newer perks are always sold. Without VisitAPI there are no quests and everything is sold from the start. Debug option in F12: *Perks for sale without quests*.

### Achievements
**127 native EFT achievements** (Profile > Achievements), rewards by mail.

### How to use
1. Get a perk (Therapist, raid, hideout, machines).
2. Main menu: **RESEARCH** button (bottom bar) or **F9** to see progress, equip augments and switch modes.
3. Drink it in raid. Press **ESC** in raid to see your level and challenges.

### Installation
Extract the zip **over your SPT folder** (the one with `EscapeFromTarkov.exe`). It creates:
- `BepInEx/plugins/TarkovaCola/` (client)
- `SPT_Runtime/user/mods/TarkovaCola/` (server)
- `SPT_Runtime/user/mods/VisitAPI-Server/packs/TarkovaCola/` (quests; only active with VisitAPI)

Progress is saved per profile in `user/mods/TarkovaCola/profiles/` (keep it when updating). **Updating from 1.x, 2.x or 3.x:** your progress is kept.

### Configuration (F12)
- **General:** language, Research hotkey, leaving the raid doesn't count as dying, *Pause perk effects*.
- **HUD:** perk icons bottom-center, perk icons over enemies, perk carrier alert.
- **Audio:** jingle, voice, perk jingles and sound effects volume.
- **Visuals:** PHD and web effects, Death Perception outline opacity, Black Ops drinking and melee animations, can size in hand.
- **Perks:** random perk chance, perks as injectors, Vulture Aid re-roll key, Mule Kick mouse wheel, perk effects on enemies and teammates.
- **Machines:** power switch, automatic placement (Fixed / Every raid / Off), redo automatic machines, machine editor, Mystery Box attachment key, TEDD chance.
- **Arena** and **Debug** (guards always, perks for sale without quests, verbose log).

### Uninstall
Delete the `TarkovaCola` folders (and the VisitAPI pack). If you still hold perks in your stash, SPT lets you remove broken items.

**Mule Kick:** before uninstalling, move the weapon out of the Mule Kick slot (3rd weapon slot) into your stash. Without the mod that slot no longer exists, and SPT will treat the weapon in it as a broken item.

<details>
<summary><b>Older versions (3.0.1 and earlier)</b></summary>

#### Hotfix 3.0.1
- **APEX Core:** dragging it in the inventory right after picking it up broke the UI. It is now absorbed when you close the inventory.
- **Perks as injectors:** the "Use" button was greyed out. Fixed by Kaganari (PR #1, thanks!).
- **Akula push dagger** (and infected hands): no longer use the melee styles; the weapon no longer floats in the air.
- **Eternal and Classic perks:** at raid start they are checked to still be active, and Deadshot is reapplied to your weapon.

#### What's new in 3.0
The biggest update so far: 3 new perks, perk machines on the map, the Mystery Box, TEDD, APEX, two Black Ops 7 weapons and perks for enemies and teammates. The Black Ops 6/7 models are shipped compiled only, for playing.

##### What's new

###### 3 new perks
| Perk | Base effect | How to get it |
|---|---|---|
| **Bandolier Bandit** | Magazines in your rig and pockets refill 1 round every 2 s from loose ammo you carry, while you are not shooting. | Therapist (7 GP), raid loot and the Wunderfizz. |
| **Vulture Aid Elixir** | Unsearched containers and bodies with valuables glow through walls (15 m). | Same as Bandolier. |
| **Melee Macchiato** | Melee damage ×2 and melee attacks 30% faster. | Same as Bandolier. |

Each one has its full **Research** tree, with augments, drawbacks, challenges and its own jingle. Vulture Aid has the **Re-roll** augment: look at a container up close, press **J** (configurable in F12) and its loot is rolled again.

###### Perk machines on the map (Factory for now, day and night)
- Perk-a-Cola machines are **on the map**. Hold **F** in front of one to buy its perk for **30,000 ₽**. If you already have it, make it **Eternal** for **15,000 ₽**.
- **Turn on the power:** machines don't work until you flip the map's power switch. Can be turned off in F12.
- **Wunderfizz:** a random perk for **40,000 ₽**, with its roulette.

###### Mystery Box
- Spawns at **one** of its spots on the map each raid. Costs **1,000 ₽**.
- **Hold F** for a **random weapon** (built, with its preset). **Hold Y** (configurable in F12) for a **random attachment**.
- Just like Zombies: the lid opens, models cycle and the prize floats. Grab it with EFT's F before it sinks back.
- After several uses in the same spot the **teddy bear** may show up: you get your money back and the box flies off to another spot.
- 15% chance to give one of the new Black Ops 7 weapons.

###### TEDD
- Shows up in **some raids** (15% by default, configurable in F12) at one of his spots. He glows green and is marked on screen when you are close.
- Talk to him (**hold F**) and he gives you **one random trial**, with 5 minutes to complete it:
  - **Item:** an item spawns far away; bring it to him.
  - **Takedown:** a PMC spawns, marked on screen; it has to die.
  - **Hold out:** 90 s near TEDD while waves of infected come in.
  - **Money:** pay him the amount he asks for.
- **Reward:** a random perk bottle.

###### APEX
A **very strong** effect for each perk. You can only have **one** equipped (Modern and Classic) and it works while its perk is active.
1. Get an **APEX Core**: rarely found in safes, caches, weapon boxes and PMC bodies. Mechanic (level 1) also sells it for 5,000,000 ₽, and it shows up in the flea market search. To count, pick it up or **carry it into a raid** and **extract alive**.
2. In **Research > APEX**, spend a core to start an APEX challenge. It only progresses while its perk is active.
3. Beat the challenge and equip it.

| APEX | Effect |
|---|---|
| Juggernog | Immune to melee and no bleeding |
| Speed Cola | Your weapon never jams |
| Stamin-Up | Infinite stamina and +10% sprint speed |
| PHD Flopper | Every kill sets off an explosion (it doesn't hurt you) |
| Widow's Wine | Your explosives go off on their own when an enemy walks close (3 m) |
| Deadshot | +10 penetration (one armor class higher) |
| Bandolier | A kill refills your magazine |
| Vulture Aid | 50% of enemies carry a valuable or quest item |
| Melee Macchiato | You take no damage while swinging |

###### Black Ops 7 weapons
- **MK35 ISR** (assault rifle, based on the HK416) and **VMP** (SMG, based on the MP5).
- Their own models, usable iron sights, and they take **their base weapon's attachments** and camos.
- **How to get them:** Mechanic (level 1), already built, or the Mystery Box.

###### Perks on enemies and teammates
- **Enemies:** those carrying a perk **get its effect**. Juggernog takes twice the damage, Speed Cola fires faster, Deadshot knows where you are through walls, Widow's Wine slows you when it hits, etc. Enemies with Bandolier or Vulture Aid spawn with extra ammo or a valuable item.
- **PIT Fireteam teammates:** when you open their inventory to give or swap gear, a panel with **4 perk slots** shows up at the bottom.
  - Press **+** and pick a perk you carry: it gets equipped and the bottle is used up when you close the inventory.
  - Press an equipped perk to take it back: the bottle returns to your gear.
  - Perks stay between raids until the teammate **dies**.
- Everything can be turned off in F12: *Perk effects on enemies and teammates*.

###### Other
- **Perks as injectors** (F12, off by default): perks become injectors with the injection animation instead of cans. Requires restarting the server and the game.
- **Melee styles:** combos depending on the weapon. Light ones do horizontal, vertical and stab; medium ones horizontal, backhand and vertical; heavy ones a single weighty swing. Knives sit closer to the screen center.
- **Refreshed UI** in Research and the raid panel: animated background, bubbles, corner brackets, hover shine and a color that follows the selected perk.
- **New mod icon.**
- **Developer options** (clip recording, debugging) hidden in F12. Turn on *Advanced settings* to see them.


##### What changed
- **Drawback revision:** drawbacks that were generic penalties (hunger, thirst, sprinting, drawing your weapon) now punish in the same area their perk helps with. Whatever you had equipped stays equipped; only the effect changes. PHD Flopper gains the **Explosive Drop** augment and the **Powder Junkie** drawback.
- **Black Ops drinking animation:** now **off** and moved to F12 > Debug as experimental. It mixed with EFT's (black hands and a floating can), so now only EFT's plays.
- PIT teammates' perks are no longer read from their backpack: they are equipped in their 4 slots.


##### Coming next
- **Machines, box and TEDD on every map.** Maps without hand-placed machines get them automatically (against walls, near loot; F12 > Machines > *Automatic machine placement*: Fixed / EveryRaid / Off). Place your own anywhere with the **machine editor** (F12 > Machines, or **End** in raid): pick the piece (perk machines, Der Wunderfizz, Mystery Box spot, TEDD spot, power switch) with PageUp/PageDown, rotate with [ ], place with Insert, delete the nearest with Delete. Hand-placed pieces win over automatic ones.
- New quests: APEX (3), Mystery Box (1) and TEDD (2).
- Fix the Black Ops drinking animation.
- Mystery Box sounds.
- Master of Movement (PHD slide) and a stats tab.

#### Hotfix 2.2.1
- Compatibility with mods that restore your equipment on death (e.g. Fair Equipment Restoration): bottle refunds (Eternal / Classic / Widow's Bite) and achievement rewards are now sent after the raid ends, never during it.
- Achievements unlocked when you die are now handled correctly.

#### What's new in 2.2
- **Fix (important):** reaching level 5 on a perk could show a blank error at raid start and break other mods' raid start (HUD mods, loot counters). Raid start is now fully isolated from errors, and achievements are checked once your character is loaded.
- **Arena Mode compatibility:** its Zombies mode drops a random perk every 5 waves and perks gain slow progress. Arena matches never count for your SPT profile (Classic and Eternal perks are kept). Options in F12 > Arena.
- **Many more perks in the world:** medical cases, weapon boxes, duffle bags, dead Scavs, buried caches, drawers and jackets, and as **loose loot** wherever drinks can spawn. Higher chances everywhere (PMCs 10%, Scavs 3%, Rogues 12%). Your old `loot.json` is replaced (backup in `loot.json.old`).
- **Augment review:** augments that cancelled their own drawback, duplicated challenges and drawbacks that didn't stack were reworked (Deadshot, Juggernog, Speed Cola, Stamin-Up, PHD Flopper, Widow's Wine). Changed challenges restart their progress if they weren't done.
- **Dead Aim** now keeps your weapon locked to the center: no sway while moving, turning or aiming (recoil still applies).
- **Quest images** for all 10 storyline quests (they were missing).
- ???

#### What's new in 2.1
- **F12 menu in English** (or Spanish): option names follow the mod language (Auto = game language). Your saved settings are kept.
- **Option to hide the perk icons** at the bottom of the screen (F12 > HUD > Show perk icons).
- **Perk carrier alert:** a notice when you get close to an enemy carrying perks (range in F12).
- **Perks show up in more places:** wooden crates, ammo boxes, grenade boxes (Widow's Wine / PHD Flopper), ration crates (Juggernog), PMC bodies and safes (almost always). **Bosses always carry 2 perks**; some PMCs, Rogues and (rarely) Scavs carry one. Chances are in `user/mods/TarkovaCola/loot.json`.
- **Fix:** perks were added to the wrong containers (dead Scavs and jackets instead of ammo boxes and safes).
- Quest items now look like what they are (notebook, envelope, sample...).

</details>

---

MIT License (código / code)

**Créditos / Credits**
- Speed Cola 3D model: "Speedcola CoD Zombies" by **albertjuli** ([Sketchfab](https://skfb.ly/6VRDU)), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Converted to a Unity asset bundle, textures re-prepared, rescaled and rotated.
- Other bottles (Stamin-Up, Juggernog, PHD Flopper, Widow's Wine, Deadshot Daiquiri): "cod zombies perks" by **Unknown Dev** ([Sketchfab](https://sketchfab.com/3d-models/cod-zombies-perks-77e9cb71adaf44b5a974c584f96cfc6c)), Sketchfab Free Standard license. Only the compiled asset bundles are included, not the original model files.
- "le creatura": fan-made M4 SOPMOD II Jr. model (Girls' Frontline, © MICA Team / Sunborn); throw sounds supplied by the mod author. Only the compiled asset bundle is included.
- Black Ops 6 / Black Ops 7 weapons, cans, machines, animations and sounds: Call of Duty, property of Activision. **Shipped as compiled asset bundles only**, for playing.
- Icons, jingles, announcer voice lines and the challenge sound: Call of Duty, property of their owners (not MIT).
- Ray Gun, Ray Gun Mark II and Wunderwaffe DG-2 are **Kenner's mods** and are **not included**: Tarkova-Cola only puts them in the Mystery Box when they are installed.
- Kaganari for the injector fix (PR #1).
- mattpsvreis for the Maps Expanded support (PR #2).
