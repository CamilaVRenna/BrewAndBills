using UnityEngine;
using System.Collections.Generic;

public class FrascoPocion : MonoBehaviour
{
    [Header("Apariencia")]
    public Material materialVacio; // Material cuando está vacío
    public Material materialLleno; // Material base cuando está lleno
    public Color colorPocionDefecto = Color.magenta; // Color si no se determina por receta

    // Datos internos
    private List<DatosIngrediente> ingredientesContenidos = null;
    private MeshRenderer renderizadorMalla; // Para cambiar el material/color
    //private bool estaSostenido = false; // ¿Lo tiene el jugador en la mano? (Necesitaría más lógica)

    // Awake se llama cuando se crea el objeto
    void Awake()
    {
        renderizadorMalla = GetComponent<MeshRenderer>(); // Obtiene el componente para cambiar apariencia
        EstablecerApariencia(false); // Asegurarse de que empieza vacío visualmente
    }

    // Llamado por InteraccionJugador cuando se recoge la poción del caldero
    public void Llenar(DatosIngrediente[] ingredientes)
    {
        // Guarda una copia de los ingredientes
        ingredientesContenidos = new List<DatosIngrediente>(ingredientes);
        // Cambia la apariencia para mostrar que está lleno
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
            // --- Lógica para determinar color basado en ingredientes ---
            // ¡Esta parte es donde defines tus "recetas" visuales!
            Color colorPocion = DeterminarColorPocion();
            // Asume que el material usa la propiedad de color estándar "_Color"
            renderizadorMalla.material.color = colorPocion;
        }
        else
        {
            renderizadorMalla.material = materialVacio; // Asigna el material de "vacío"
            // Podrías querer resetear el color también si usas el mismo material base
            // renderizadorMalla.material.color = Color.white; // O el color original del material vacío
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

        // --- LÓGICA CORREGIDA ---
        if (!lleno) // Solo cambiar el material si NO está lleno (o sea, si se vacía)
        {
            renderizadorMalla.material = materialVacio;
            // Debug.Log($"Frasco {gameObject.name}: Aplicando material VACÍO."); // Log opcional
        }
        // Si lleno == true, NO hacemos NADA aquí con el material.
        // Dejamos el material que InteraccionJugador.LlenarFrascoSostenido ya puso.
        // --- FIN LÓGICA CORREGIDA ---
    }

    // Función para decidir el color. ¡Aquí es donde pones tu lógica de recetas!
    /* Color DeterminarColorPocion()
     {
         if (ingredientesContenidos == null || ingredientesContenidos.Count == 0)
         {
             return colorPocionDefecto; // Devuelve color por defecto si está vacío (aunque no debería llamarse)
         }

         // ----- EJEMPLOS DE LÓGICA DE RECETAS VISUALES -----
         // Puedes hacer esto tan simple o complejo como quieras

         // Ejemplo 1: Basado en el primer ingrediente
         // if (ingredientesContenidos[0].nombreIngrediente == "Plumas") return Color.cyan;
         // if (ingredientesContenidos[0].nombreIngrediente == "Miel") return Color.yellow;

         // Ejemplo 2: Basado en si contiene un ingrediente específico
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

    // Llamado por InteraccionJugador cuando interactúa con el frasco (si estuviera en el mundo)
    public void Interactuar(InteraccionJugador jugador, Caldero caldero)
    {
        // Esta función necesitaría más lógica dependiendo de si el frasco está en el mundo o en la mano.
        // Por ahora, la lógica de llenado está principalmente en InteraccionJugador y Caldero.
        // Si el frasco estuviera en el mundo, aquí comprobarías si está vacío y el caldero listo,
        // y si es así, le dirías al jugador que lo recoja y lo llene.
    }

    // Llamado por InteraccionJugador (ej: clic derecho) para ver qué contiene
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
                    textoContenido += ", "; // Añade coma entre ingredientes
                }
            }
            // Muestra el contenido en la consola o en una UI de notificación
            Debug.Log(textoContenido);
            InteraccionJugador interaccion = FindObjectOfType<InteraccionJugador>(); // Encuentra la interacción para mostrar UI
            if (interaccion) interaccion.MostrarNotificacion(textoContenido, 4f); // Muestra por más tiempo
        }
        else
        {
            Debug.Log("Este frasco está vacío.");
            InteraccionJugador interaccion = FindObjectOfType<InteraccionJugador>();
            if (interaccion) interaccion.MostrarNotificacion("Este frasco está vacío.");
        }
    }
}