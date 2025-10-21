using UnityEngine;
using System.Collections.Generic;

public class FrascoPocion : MonoBehaviour
{
    [Header("Apariencia")]
    public Material materialVacio; // Material cuando est� vac�o
    public Material materialLleno; // Material base cuando est� lleno
    public Color colorPocionDefecto = Color.magenta; // Color si no se determina por receta

    // Datos internos
    private List<DatosIngrediente> ingredientesContenidos = null;
    private MeshRenderer renderizadorMalla; // Para cambiar el material/color
    //private bool estaSostenido = false; // �Lo tiene el jugador en la mano? (Necesitar�a m�s l�gica)

    // Awake se llama cuando se crea el objeto
    void Awake()
    {
        renderizadorMalla = GetComponent<MeshRenderer>(); // Obtiene el componente para cambiar apariencia
        EstablecerApariencia(false); // Asegurarse de que empieza vac�o visualmente
    }

    // Llamado por InteraccionJugador cuando se recoge la poci�n del caldero
    public void Llenar(DatosIngrediente[] ingredientes)
    {
        // Guarda una copia de los ingredientes
        ingredientesContenidos = new List<DatosIngrediente>(ingredientes);
        // Cambia la apariencia para mostrar que est� lleno
        //EstablecerApariencia(true);
        Debug.Log($"Frasco llenado con {ingredientesContenidos.Count} ingredientes.");
    }

    // Cambia el material y/o color del frasco
    /*public void EstablecerApariencia(bool lleno)
    {
        if (renderizadorMalla == null) return; // Salir si no hay MeshRenderer

        if (lleno)
        {
            renderizadorMalla.material = materialLleno; // Asigna el material de "lleno"
            // --- L�gica para determinar color basado en ingredientes ---
            // �Esta parte es donde defines tus "recetas" visuales!
            Color colorPocion = DeterminarColorPocion();
            // Asume que el material usa la propiedad de color est�ndar "_Color"
            renderizadorMalla.material.color = colorPocion;
        }
        else
        {
            renderizadorMalla.material = materialVacio; // Asigna el material de "vac�o"
            // Podr�as querer resetear el color tambi�n si usas el mismo material base
            // renderizadorMalla.material.color = Color.white; // O el color original del material vac�o
        }
    }*/

    public void EstablecerApariencia(bool lleno)
    {
        if (renderizadorMalla == null)
        {
            // Intentar encontrarlo de nuevo por si acaso se asigna tarde
            renderizadorMalla = GetComponentInChildren<MeshRenderer>();
            if (renderizadorMalla == null)
            {
                Debug.LogError("FrascoPocion no tiene MeshRenderer.", gameObject);
                return; // Salir si no hay renderer
            }
        }

        // --- L�GICA CORREGIDA ---
        if (!lleno) // Solo cambiar el material si NO est� lleno (o sea, si se vac�a)
        {
            renderizadorMalla.material = materialVacio;
            // Debug.Log($"Frasco {gameObject.name}: Aplicando material VAC�O."); // Log opcional
        }
        // Si lleno == true, NO hacemos NADA aqu� con el material.
        // Dejamos el material que InteraccionJugador.LlenarFrascoSostenido ya puso.
        // --- FIN L�GICA CORREGIDA ---
    }

    // Funci�n para decidir el color. �Aqu� es donde pones tu l�gica de recetas!
    /* Color DeterminarColorPocion()
     {
         if (ingredientesContenidos == null || ingredientesContenidos.Count == 0)
         {
             return colorPocionDefecto; // Devuelve color por defecto si est� vac�o (aunque no deber�a llamarse)
         }

         // ----- EJEMPLOS DE L�GICA DE RECETAS VISUALES -----
         // Puedes hacer esto tan simple o complejo como quieras

         // Ejemplo 1: Basado en el primer ingrediente
         // if (ingredientesContenidos[0].nombreIngrediente == "Plumas") return Color.cyan;
         // if (ingredientesContenidos[0].nombreIngrediente == "Miel") return Color.yellow;

         // Ejemplo 2: Basado en si contiene un ingrediente espec�fico
         bool tieneFlores = false;
         bool tieneMiel = false;
         foreach (var ingrediente in ingredientesContenidos)
         {
             if (ingrediente.nombreIngrediente.ToLower().Contains("flores")) tieneFlores = true;
             if (ingrediente.nombreIngrediente.ToLower().Contains("miel")) tieneMiel = true;
         }

         if (tieneFlores && tieneMiel) return Color.green; // Flores + Miel = Verde
         if (tieneFlores) return Color.yellow;             // Solo Flores = Amarillo
         if (tieneMiel) return Color.red;                  // Solo Miel = Rojo

         // Si ninguna regla coincide, usa el color por defecto
         return colorPocionDefecto;
     }*/

    // Llamado por InteraccionJugador cuando interact�a con el frasco (si estuviera en el mundo)
    public void Interactuar(InteraccionJugador jugador, Caldero caldero)
    {
        // Esta funci�n necesitar�a m�s l�gica dependiendo de si el frasco est� en el mundo o en la mano.
        // Por ahora, la l�gica de llenado est� principalmente en InteraccionJugador y Caldero.
        // Si el frasco estuviera en el mundo, aqu� comprobar�as si est� vac�o y el caldero listo,
        // y si es as�, le dir�as al jugador que lo recoja y lo llene.
    }

    // Llamado por InteraccionJugador (ej: clic derecho) para ver qu� contiene
    public void MostrarContenido()
    {
        if (ingredientesContenidos != null && ingredientesContenidos.Count > 0)
        {
            string textoContenido = "Este frasco contiene: ";
            // Construye la cadena con los nombres de los ingredientes
            for (int i = 0; i < ingredientesContenidos.Count; i++)
            {
                textoContenido += ingredientesContenidos[i].nombreIngrediente;
                if (i < ingredientesContenidos.Count - 1)
                {
                    textoContenido += ", "; // A�ade coma entre ingredientes
                }
            }
            // Muestra el contenido en la consola o en una UI de notificaci�n
            Debug.Log(textoContenido);
            InteraccionJugador interaccion = FindObjectOfType<InteraccionJugador>(); // Encuentra la interacci�n para mostrar UI
            if (interaccion) interaccion.MostrarNotificacion(textoContenido, 4f); // Muestra por m�s tiempo
        }
        else
        {
            Debug.Log("Este frasco est� vac�o.");
            InteraccionJugador interaccion = FindObjectOfType<InteraccionJugador>();
            if (interaccion) interaccion.MostrarNotificacion("Este frasco est� vac�o.");
        }
    }
}