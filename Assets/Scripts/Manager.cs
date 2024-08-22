using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public OSC osc;

    public GameObject RoomMesh;

    public GameObject CropBox;


    // Start is called before the first frame update
    void Start()
    {

    
       osc.SetAddressHandler("/CreateCropBox",receiveCropBoxes );
        
    }


    void receiveCropBoxes(OscMessage message){


         GameObject g =Instantiate(CropBox,(Vector3) message.values[1],(Quaternion) message.values[2]);


        g.transform.localScale = (Vector3)message.values[3];

        g.name =message.values[0].ToString() ;

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
